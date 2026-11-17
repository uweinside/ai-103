# Requires PowerShell 7 and Azure CLI. This is setup/ingestion, NOT the RAG loop.
# It provisions a dedicated Search service, sample-owned knowledge objects, a
# project connection and the resource-scoped roles described in README.md.
[CmdletBinding()]
param(
    [ValidatePattern('^[a-z0-9][a-z0-9-]+$')][string]$SearchService
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$envFile = Join-Path $root '.env'
if (Test-Path $envFile) {
    foreach ($line in Get-Content $envFile) {
        if ($line.Trim() -eq '' -or $line.Trim().StartsWith('#')) { continue }
        $pair = $line.Split('=', 2)
        if ($pair.Length -eq 2) { [Environment]::SetEnvironmentVariable($pair[0].Trim(), $pair[1].Trim()) }
    }
}

function Invoke-AzJson {
    param([string[]]$Arguments)
    $result = & az @Arguments --only-show-errors --output json
    if ($LASTEXITCODE -ne 0) { throw "Azure CLI failed: $($Arguments[0..1] -join ' '). See the CLI error above." }
    if ($result) { ($result -join "`n") | ConvertFrom-Json }
}
function Invoke-Search {
    param([string]$Method, [string]$Path, $Body, [string]$File)
    # Tokens stay in memory/HTTP headers, never in URLs, files or console output.
    # Acquire per request so a long setup can refresh its credentials.
    $token = Invoke-AzJson -Arguments @('account','get-access-token','--resource','https://search.azure.com')
    $headers = @{ Authorization = "Bearer $($token.accessToken)"; Prefer = 'return=representation' }
    $request = @{
        Method = $Method; Uri = "$searchEndpoint/$($Path)?api-version=$api"
        Headers = $headers; TimeoutSec = 200
    }
    if ($File) {
        $headers['Content-Disposition'] = "attachment; filename=`"$([IO.Path]::GetFileName($File))`""
        $request.InFile = $File
        $request.ContentType = 'application/octet-stream'
    } elseif ($null -ne $Body) {
        $request.Body = $Body | ConvertTo-Json -Depth 20
        $request.ContentType = 'application/json'
    }
    Invoke-RestMethod @request
}
function Ensure-Role {
    param([string]$Principal, [string]$Scope, [string]$Role)
    $existing = @(Invoke-AzJson -Arguments @('role','assignment','list','--scope',$Scope,
        '--assignee-object-id',$Principal,'--include-inherited'))
    if (-not ($existing | Where-Object roleDefinitionName -eq $Role)) {
        Write-Host "[permissions] Granting $Role at the required resource scope."
        $null = Invoke-AzJson -Arguments @('role','assignment','create','--assignee-object-id',$Principal,
            '--assignee-principal-type','ServicePrincipal','--role',$Role,'--scope',$Scope)
        $script:rolesChanged = $true
    }
}

if (-not $env:FOUNDRY_PROJECT_ENDPOINT -or -not $env:FOUNDRY_MODEL) {
    throw 'Set FOUNDRY_PROJECT_ENDPOINT and FOUNDRY_MODEL in .env first; see SETUP.md.'
}
$projectUri = [uri]$env:FOUNDRY_PROJECT_ENDPOINT
if ($projectUri.Scheme -ne 'https' -or $projectUri.AbsolutePath -notmatch '^/api/projects/([^/]+)/?$') {
    throw 'FOUNDRY_PROJECT_ENDPOINT must be an HTTPS Foundry project URL ending /api/projects/PROJECT.'
}
$projectName = $Matches[1]
$accountName = $projectUri.Host.Split('.')[0]
$account = @(Invoke-AzJson -Arguments @('cognitiveservices','account','list') |
    Where-Object name -eq $accountName)
if ($account.Count -ne 1) { throw 'Cannot find the configured Foundry account in the active Azure subscription.' }
$account = $account[0]
$projectId = "$($account.id)/projects/$projectName"
$project = Invoke-AzJson -Arguments @('rest','--method','get','--url',
    "https://management.azure.com$($projectId)?api-version=2025-06-01")
if (-not $project.identity.principalId) {
    throw 'The Foundry project needs a managed identity. Setup will not change an existing project identity.'
}
# Pick an existing embedding deployment, never silently deploy/upgrade a model.
$deployments = @(Invoke-AzJson -Arguments @('cognitiveservices','account','deployment','list',
    '--resource-group',$account.resourceGroup,'--name',$accountName))
$embedding = $deployments | Where-Object { $_.properties.model.name -like 'text-embedding-*' } |
    Sort-Object name | Select-Object -First 1
if (-not $embedding) { throw 'Deploy a text-embedding model in the configured Foundry account before setup.' }
$chat = $deployments | Where-Object name -eq $env:FOUNDRY_MODEL
if (-not $chat) { throw 'FOUNDRY_MODEL is not deployed in the configured Foundry account.' }
$files = @('Uwes_Travel_London.pdf','Uwes_Travel_Munich.pdf','Uwes_Travel_Paris.pdf')
foreach ($file in $files) {
    if (-not (Test-Path (Join-Path $root "etc\collateral\$file"))) { throw "Missing brochure: $file" }
}

$api = '2026-08-01-preview'
# A separate Basic service avoids touching another demo's indexes, auth or
# network settings. Names are deterministic per project so reruns reuse only
# this tagged service. Basic has an ongoing cost until explicitly deleted.
$suffix = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData(
    [Text.Encoding]::UTF8.GetBytes($projectId.ToLowerInvariant()))).Substring(0, 8).ToLowerInvariant()
if (-not $SearchService) { $SearchService = "ai103-hotels-$suffix" }
$services = @(Invoke-AzJson -Arguments @('search','service','list','--resource-group',$account.resourceGroup))
$search = $services | Where-Object name -eq $SearchService
if ($search -and $search.tags.'ai103-sample' -ne '06') {
    throw 'The Search name belongs to an unrelated service; refusing to reuse or modify it.'
}
if (-not $search) {
    Write-Host "[resources] Creating dedicated Basic Search service $SearchService in $($account.resourceGroup) (billable)."
    $search = Invoke-AzJson -Arguments @('search','service','create','--resource-group',$account.resourceGroup,
        '--name',$SearchService,'--location',$account.location,'--sku','basic','--replica-count','1',
        '--partition-count','1','--identity-type','SystemAssigned','--disable-local-auth','true',
        '--semantic-search','free','--tags','ai103-sample=06')
}
if (-not $search.identity.principalId -or -not $search.disableLocalAuth) {
    throw 'The sample Search service must have a managed identity and key authentication disabled.'
}
$searchEndpoint = "https://$SearchService.search.windows.net"
# Fixed names are deliberately limited to this sample. Refuse collisions rather
# than overwrite an unrelated instructor's knowledge source, base or connection.
$sourceName = 'ai103-travel-brochures'
$baseName = 'ai103-travel-hotels'
$connectionName = 'ai103-travel-iq'
$marker = 'AI-103 sample 06: fictional travel brochures'
$mcpEndpoint = "$searchEndpoint/knowledgebases/$baseName/mcp?api-version=$api"
$rolesChanged = $false
Ensure-Role $search.identity.principalId $account.id 'Cognitive Services User'
Ensure-Role $project.identity.principalId $search.id 'Search Index Data Reader'
# Setup needs data-plane rights too. Owner alone is not Search data access.
# Read the signed-in CLI user's object ID without logging identity information.
$caller = Invoke-AzJson -Arguments @('ad','signed-in-user','show','--query','id')
$existing = @(Invoke-AzJson -Arguments @('role','assignment','list','--scope',$search.id,
    '--assignee-object-id',$caller,'--include-inherited'))
foreach ($role in @('Search Service Contributor','Search Index Data Contributor')) {
    if (-not ($existing | Where-Object roleDefinitionName -eq $role)) {
        $null = Invoke-AzJson -Arguments @('role','assignment','create','--assignee-object-id',$caller,
            '--assignee-principal-type','User','--role',$role,'--scope',$search.id)
        $rolesChanged = $true
    }
}
if ($rolesChanged) {
    Write-Host '[permissions] Waiting 60 seconds for resource-scoped role assignments to propagate.'
    Start-Sleep -Seconds 60
}
$sources = Invoke-Search -Method GET -Path 'knowledgesources'
$source = $sources.value | Where-Object name -eq $sourceName
$bases = Invoke-Search -Method GET -Path 'knowledgebases'
$base = $bases.value | Where-Object name -eq $baseName
if (($source -and $source.description -ne $marker) -or ($base -and $base.description -ne $marker)) {
    throw 'A sample object name is already in use by unrelated content; refusing to overwrite it.'
}
$connections = Invoke-AzJson -Arguments @('rest','--method','get','--url',
    "https://management.azure.com$projectId/connections?api-version=2025-10-01-preview")
$connection = $connections.value | Where-Object { ($_.name -split '/')[-1] -eq $connectionName }
if ($connection -and $connection.properties.target -ne $mcpEndpoint) {
    throw 'The sample connection name already targets a different endpoint; refusing to overwrite it.'
}

# Minimal PDF extraction is sufficient for these text-bearing, one-page PDFs:
# no OCR/Content Understanding, Blob Storage, custom chunker or indexer to teach.
# Search owns extraction, chunking, vectorization and the generated search index.
$modelEndpoint = "https://$accountName.openai.azure.com"
$null = Invoke-Search -Method PUT -Path "knowledgesources/$sourceName" -Body @{
    name = $sourceName; kind = 'file'; description = $marker
    fileParameters = @{ ingestionParameters = @{
        contentExtractionMode = 'minimal'
        embeddingModel = @{ kind = 'azureOpenAI'; azureOpenAIParameters = @{
            resourceUri = $modelEndpoint; deploymentId = $embedding.name; modelName = $embedding.properties.model.name
        }}
    }}
}
$uploaded = @( (Invoke-Search -Method GET -Path "knowledgesources/$sourceName/files").value )
foreach ($file in $files) {
    $matches = @($uploaded | Where-Object fileName -eq $file)
    if ($matches.Count -gt 1) { throw "Duplicate remote files named $file; inspect the knowledge source before rerunning." }
    if ($matches.Count -eq 1) {
        if ($matches[0].errorMessage) { throw "Previously uploaded $file has an ingestion error: $($matches[0].errorMessage)" }
        Write-Host "[ingestion] Already uploaded: $file (delete the remote file explicitly to replace it)."
        continue
    }
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        Write-Host "[ingestion] Uploading and indexing $file (attempt $attempt/3) ..."
        try {
            $result = Invoke-Search -Method POST -Path "knowledgesources/$sourceName/files" -File (Join-Path $root "etc\collateral\$file")
            break
        } catch [Microsoft.PowerShell.Commands.HttpResponseException] {
            $status = [int]$_.Exception.Response.StatusCode
            $transient = $status -in @(408,429,500,502,503,504) -or $_.ErrorDetails.Message -like '*Operation was canceled*'
            if (-not $transient -or $attempt -eq 3) { throw }
            Write-Warning "Azure upload returned HTTP $status; waiting 15 seconds before retrying $file."
            Start-Sleep -Seconds 15
            # POST is not idempotent: first check whether Azure finished the
            # upload despite the transient response. Never blindly add a copy.
            $remote = @((Invoke-Search -Method GET -Path "knowledgesources/$sourceName/files").value |
                Where-Object fileName -eq $file)
            if ($remote.Count -gt 1) { throw "Duplicate remote files named $file; inspect before rerunning." }
            if ($remote.Count -eq 1) {
                $result = $remote[0]
                break
            }
        }
    }
    if (-not $result.fileId -or $result.errorMessage) { throw "File ingestion did not succeed: $file" }
}
# Keep retrieval extractive: one final answering model, no extra query-planning
# LLM. This is still a real reusable Foundry IQ knowledge base, not file_search.
$null = Invoke-Search -Method PUT -Path "knowledgebases/$baseName" -Body @{
    name = $baseName; description = $marker
    knowledgeSources = @(@{ name = $sourceName })
    retrievalReasoningEffort = @{ kind = 'minimal' }
    outputMode = 'extractiveData'
}
$connectionBody = @{
    name = $connectionName
    properties = @{
        authType = 'ProjectManagedIdentity'; category = 'RemoteTool'; target = $mcpEndpoint
        isSharedToAll = $true; audience = 'https://search.azure.com/'; metadata = @{ ApiType = 'Azure' }
    }
} | ConvertTo-Json -Depth 10
# Send JSON in memory rather than as an az.cmd argument: Windows command-line
# quote parsing must not corrupt the connection definition or expose tokens.
$armToken = Invoke-AzJson -Arguments @('account','get-access-token','--resource','https://management.azure.com/')
$null = Invoke-RestMethod -Method PUT -Uri "https://management.azure.com$projectId/connections/$($connectionName)?api-version=2025-10-01-preview" `
    -Headers @{ Authorization = "Bearer $($armToken.accessToken)" } -ContentType 'application/json' -Body $connectionBody

# Persist only non-secret app settings, preserving all other .env entries. A
# failed upload/connection never writes a success-shaped runtime configuration.
$settings = @{
    FOUNDRY_IQ_SEARCH_ENDPOINT = $searchEndpoint
    FOUNDRY_IQ_KNOWLEDGE_BASE = $baseName
    FOUNDRY_IQ_CONNECTION_NAME = $connectionName
}
$lines = if (Test-Path $envFile) { @(Get-Content $envFile) } else { @() }
foreach ($key in $settings.Keys) {
    $lines = @($lines | Where-Object { $_ -notmatch "^\s*$key\s*=" })
    $lines += "$key=$($settings[$key])"
    [Environment]::SetEnvironmentVariable($key, $settings[$key])
}
Set-Content -LiteralPath $envFile -Value $lines -Encoding utf8
Write-Host "[ready] Three brochures indexed. Run .\scripts\run.ps1 6"

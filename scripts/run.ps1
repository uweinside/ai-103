param(
  [ValidateSet('1','1b','2','3','4','5','6')][string]$Sample = '1',
  [Parameter(ValueFromRemainingArguments)][string[]]$SampleArguments = @()
)
Set-Location (Join-Path $PSScriptRoot '..')
if (Test-Path .env) {
  foreach ($line in Get-Content .env) {
    $value = $line.Trim()
    if ($value -eq '' -or $value.StartsWith('#')) { continue }
    $parts = $value.Split('=', 2)
    if ($parts.Length -eq 2) { [Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim()) }
  }
}
$folders = @{'1'='01-DirectResponses';'1b'='01B-ResponsesSdk';'2'='02-ManualToolCalling';'3'='03-FoundryManagedAgent';'4'='04-AgentFrameworkWorkflow';'5'='05-McpClientAgent';'6'='06-FoundryIqRag'}
dotnet run --project $folders[$Sample] -- @SampleArguments
exit $LASTEXITCODE

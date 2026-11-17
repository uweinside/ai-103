# Environment setup and authentication

## Variables

Copy `.env.example` to `.env` and replace placeholders. `.env` is ignored by Git. The console apps intentionally read **environment variables**, not `.env` themselves, to explain the boundary between application configuration and secret management.

The example selects `gpt-5.4-mini` for the apps, including 01B. Replace both deployment-name values if your deployment has a different name. The model must be available to the Foundry project and support the Responses API and strict function calling. Check your region and quota.

| Variable | Apps | Meaning |
|---|---|---|
| `AZURE_TOKEN_CREDENTIALS` | 01–05 | Set to `AzureCliCredential` in the example `.env` so `DefaultAzureCredential` uses the account from `az login` rather than another local developer identity. |
| `AZURE_OPENAI_ENDPOINT` | 01–02 | Foundry/Azure OpenAI **resource root** (e.g. `https://my-resource.services.ai.azure.com` or `https://my-resource.openai.azure.com`); app appends `/openai/v1/responses`. Do not put `/openai/v1/` into this value. |
| `AZURE_OPENAI_DEPLOYMENT_NAME` | 01–02 | Your deployment name, not necessarily the base model's name. |
| `FOUNDRY_PROJECT_ENDPOINT` | 03–05 | Foundry project URL, e.g. `https://my-resource.services.ai.azure.com/api/projects/my-project`. |
| `FOUNDRY_MODEL` | 03–05 | Model deployment name associated with the Foundry project. |
| `KEEP_DEMO_AGENT` | 03 | Set `true` to leave its created agent in Foundry; otherwise sample cleans up its created version. |

> If your Foundry project exposes a **project-scoped OpenAI v1 endpoint** rather than an Azure OpenAI resource-root endpoint, verify the route in your project's **Models / Endpoints** UI. Do not blindly concatenate paths. Apps 01–02 teach the resource-root `/openai/v1/responses` route.

**App 01B** reuses `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_DEPLOYMENT_NAME` and
`AZURE_TOKEN_CREDENTIALS` unchanged. It adds `/openai/v1/` to the resource root
for `ResponsesClient`; the SDK appends `responses`. Do not configure a full
operation URL or a project endpoint for this sample.

## Authentication

```bash
az login
az account show
```

The Azure CLI command is **`az login`**, not `az auth login`. Use `az login --tenant <tenant-id>` when you need to select a specific tenant.

**All apps use `DefaultAzureCredential` and Microsoft Entra ID for model access; there is no API-key authentication path.** The example `.env` sets `AZURE_TOKEN_CREDENTIALS=AzureCliCredential` to restrict the chain to the identity signed in through Azure CLI. Without this setting, the default chain can select another configured identity (such as environment credentials, managed identity or a supported developer tool) before Azure CLI; `az account show` then describes the CLI account, not necessarily the identity selected by the chain. For a different hosting environment, change or remove the selector and configure the intended identity and permissions.

**Apps 01–02:** explicitly acquire a bearer token for `https://cognitiveservices.azure.com/.default` before every REST request, including the tool-result turn. The reused credential caches and refreshes tokens; plain `HttpClient` does not do this itself.  
**Apps 03–05:** pass the credential to Foundry's `AIProjectClient`; the SDK handles token acquisition and refresh for its service endpoints.

**App 01B:** passes the credential to the OpenAI SDK's `BearerTokenPolicy` with
the same `https://cognitiveservices.azure.com/.default` audience as app 01.
The SDK policy acquires/refreshes tokens; no manual bearer header or API key
is needed. The application's choice of identity and the service's RBAC checks
remain unchanged.

**App 05 MCP connection:** Microsoft Learn's public endpoint requires no credentials.
Only the separate agent/model mode needs Azure configuration. To discover tools
and run a public documentation search without Azure, use
`dotnet run --project .\05-McpClientAgent -- --tools-only`.
See [sample 05](05-McpClientAgent/README.md) for the client/server walkthrough.

Authentication failures propagate without an API-key fallback. Tokens and credentials must never be printed or placed in `.env`. Remove any old `AZURE_OPENAI_API_KEY` entry from local configuration; it is no longer read.

Your Azure identity must have permission to invoke the deployment; persisted agent creation additionally needs Foundry agent management permission. Authentication failures are not evidence that code/model behavior is incorrect.

## Load `.env` (optional)

### PowerShell

```powershell
./scripts/run.ps1 1
./scripts/run.ps1 1b
./scripts/run.ps1 2
./scripts/run.ps1 3
./scripts/run.ps1 4
./scripts/run.ps1 5
```

If `RemoteSigned` blocks the launcher because Windows marked it as downloaded, inspect the script and then unblock only that trusted file:

```powershell
Unblock-File -LiteralPath .\scripts\run.ps1
```

This removes the downloaded-file marker without changing the execution policy.

If your PowerShell execution policy blocks unsigned scripts, load the configuration in the current terminal and run a project directly instead of changing that policy:

```powershell
Get-Content .env | ForEach-Object {
    $line = $_.Trim()
    if ($line -and -not $line.StartsWith('#')) {
        $parts = $line.Split('=', 2)
        if ($parts.Length -eq 2) {
            [Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim())
        }
    }
}
dotnet run --project .\01-DirectResponses
```

Replace the project path to run another sample.

### Bash

```bash
bash scripts/run.sh 1
bash scripts/run.sh 1b
bash scripts/run.sh 2
bash scripts/run.sh 3
bash scripts/run.sh 4
bash scripts/run.sh 5
```

Both scripts load `.env` then call `dotnet run --project ...`. The variable values never appear in app logs.

## A useful first prompt

`What is our VPN policy, and what is the status of ticket INC-1042?`

Notice the differences: app 01 has no knowledge source and should avoid pretending otherwise; app 02 can answer ticket status **from a mocked tool**; app 03 is a simple **declarative agent without knowledge/tools**; app 04 coordinates **specialized code-defined agents**.

App 05 instead uses a fixed public question about .NET MCP integration. It
searches Microsoft Learn, not company VPN policies or ticket records. Never
send private service-desk data to its public search tool.

## Common failures

- **Credential unavailable / authentication failed:** run `az login`, check the tenant and inspect which identity `DefaultAzureCredential` selects. No API key is needed.
- **401 / 403:** wrong tenant, role, token audience, resource, or expired/revoked sign-in.
- **404:** deployment name, endpoint route, or model doesn't support the API in that region.
- **400 on tool loop:** model lacks function-calling support or tools are incorrectly shaped; inspect the JSON trace.
- **Agent creation unauthorized:** additional management permission needed.
- **NuGet restore failure:** network/proxy, package versions or feed access; do not silently upgrade everything without noting API changes.
- **.NET build error:** check exact referenced package versions, then compare current official samples in SOURCES.md.

## Explore MCP servers with the official MCP Inspector

This optional, standalone developer tool does not add MCP integration to the apps; app 05 implements its own independent MCP client in C#. The official project is **MCP Inspector** (`@modelcontextprotocol/inspector`), not a separate official "MCP Explorer" product.

The demo uses Inspector **2.14.0**. Its Node.js requirement is `^22.23.4 || ^24.22.1 || >=26.12.1`. Install it for the current user, then launch from this repository:

```powershell
npm install --global @modelcontextprotocol/inspector@2.14.0
mcp-inspector --web --config .\scripts\mcp-inspector.json
```

Open `http://127.0.0.1:6274` if the browser does not open automatically. Keep the launching terminal running; press Ctrl+C there to stop Inspector. Leave authentication enabled and the bind address on loopback. Inspector can start local processes when configured to do so; do not expose it on the network or enter credentials into a shared demonstration.

The config contains two remote **Streamable HTTP** servers, neither requiring a key or sign-in:

| Server | Endpoint | Suggested read-only demonstration |
|---|---|---|
| Microsoft Learn | `https://learn.microsoft.com/api/mcp` | Discover tools, select `microsoft_docs_search`, and supply `query`: `Microsoft Foundry Agent Framework MCP tools`. |
| DeepWiki (public repositories) | `https://mcp.deepwiki.com/mcp` | Discover tools, select `read_wiki_structure`, and supply `repoName`: `modelcontextprotocol/inspector`. |

Select a server, connect, then inspect its **Tools**, input schema, tool result, and request/response history in the monitoring sidebar's **Protocol** view. Disconnect before switching servers. Discover the current tools before calling them: the server owns their names, schemas, and execution, and those can change. Use only public example queries and public repository names; tool arguments are transmitted to the selected external service. DeepWiki is a third-party service, not a Microsoft or MCP-project server.

**Teaching boundary:** Inspector is an MCP client, not an LLM agent and not a transparent monitor for the five apps. Here the instructor chooses the tool and arguments. In an agent integration, the model requests a tool call; the client/runtime applies permissions, validates and dispatches it to the MCP server, and returns the result to the model. A tool result is context, not permission to execute instructions contained in that result. Inspect the protocol here, then contrast it with app 02's explicitly application-owned local function dispatch.

Repeat the same calls without the browser:

```powershell
mcp-inspector --cli --config .\scripts\mcp-inspector.json --server microsoft-learn --method tools/list
mcp-inspector --cli --config .\scripts\mcp-inspector.json --server microsoft-learn --method tools/call --tool-name microsoft_docs_search --tool-arg 'query=Microsoft Foundry Agent Framework MCP tools'
mcp-inspector --cli --config .\scripts\mcp-inspector.json --server deepwiki-public --method tools/call --tool-name read_wiki_structure --tool-arg 'repoName=modelcontextprotocol/inspector'
```

**Inspector v2 differences:** `--config` opens this file read-only; `--catalog` instead gives Inspector ownership of an editable server list. To add other servers through the UI without modifying the teaching configuration, copy it to a new local file outside the repository and launch with `--catalog` pointing to that copy. Review new servers' permissions and data handling before connecting; do not add filesystem/write tools or authenticated services to this demo without a separate review. v2 serves the UI and backend together on port **6274**; older v1 tutorials describing a separate proxy on 6277 and URL authentication tokens do not apply.

Troubleshooting:

- **405 when browsing the Learn endpoint:** expected; use Inspector with Streamable HTTP, not direct browser navigation or the legacy SSE transport.
- **`mcp-inspector` not found after installing:** reopen the terminal so it picks up npm's user-bin directory, or use `npx --yes @modelcontextprotocol/inspector@2.14.0 --web --config .\scripts\mcp-inspector.json`.
- **Port 6274 already in use:** do not stop an unrelated process. Set `$env:CLIENT_PORT = '6284'` in the launching terminal and browse `http://127.0.0.1:6284`.
- **Unsupported resources/prompts:** capabilities differ by server. A tool-only server need not implement resources or prompts.
- **Learn schema-portability warnings:** Inspector 2.14.0 flags nullable `type` arrays in Learn's output schemas. These are legal JSON Schema, not installation or execution errors; the demonstrated search succeeds. The schemas belong to the remote server, so do not rewrite them locally or hide the warnings. Other clients may have different schema-dialect restrictions.
- **TLS, proxy, or remote errors:** preserve certificate verification and report the actual connection/tool error. Check the service's availability and your organization's approved proxy/CA configuration; do not disable TLS checks or Inspector authentication.

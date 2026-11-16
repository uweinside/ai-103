# Environment setup and authentication

## Variables

Copy `.env.example` to `.env` and replace placeholders. `.env` is ignored by Git. The console apps intentionally read **environment variables**, not `.env` themselves, to explain the boundary between application configuration and secret management.

| Variable | Apps | Meaning |
|---|---|---|
| `AZURE_OPENAI_ENDPOINT` | 01–02 | Azure OpenAI **resource root** (e.g. `https://my-resource.openai.azure.com`); app appends `/openai/v1/responses`. Do not put `/openai/v1/` into this value. |
| `AZURE_OPENAI_DEPLOYMENT_NAME` | 01–02 | Your deployment name, not necessarily the base model's name. |
| `FOUNDRY_PROJECT_ENDPOINT` | 03–04 | Foundry project URL, e.g. `https://my-resource.services.ai.azure.com/api/projects/my-project`. |
| `FOUNDRY_MODEL` | 03–04 | Model deployment name associated with the Foundry project. |
| `KEEP_DEMO_AGENT` | 03 | Set `true` to leave its created agent in Foundry; otherwise sample cleans up its created version. |

> If your Foundry project exposes a **project-scoped OpenAI v1 endpoint** rather than an Azure OpenAI resource-root endpoint, verify the route in your project's **Models / Endpoints** UI. Do not blindly concatenate paths. Apps 01–02 teach the resource-root `/openai/v1/responses` route.

## Authentication

```bash
az login
az account show
```

The Azure CLI command is **`az login`**, not `az auth login`. Use `az login --tenant <tenant-id>` when you need to select a specific tenant.

**All four apps use `DefaultAzureCredential` and Microsoft Entra ID; there is no API-key authentication path.** Locally, the credential chain can use the identity signed in through Azure CLI. It can also select another configured identity (such as environment credentials, managed identity or a supported developer tool) before Azure CLI; `az account show` describes the CLI account, not necessarily the identity selected by the chain.

**Apps 01–02:** explicitly acquire a bearer token for `https://cognitiveservices.azure.com/.default` before every REST request, including the tool-result turn. The reused credential caches and refreshes tokens; plain `HttpClient` does not do this itself.  
**Apps 03–04:** pass the credential to Foundry's `AIProjectClient`; the SDK handles token acquisition and refresh for its service endpoints.

Authentication failures propagate without an API-key fallback. Tokens and credentials must never be printed or placed in `.env`. Remove any old `AZURE_OPENAI_API_KEY` entry from local configuration; it is no longer read.

Your Azure identity must have permission to invoke the deployment; persisted agent creation additionally needs Foundry agent management permission. Authentication failures are not evidence that code/model behavior is incorrect.

## Load `.env` (optional)

### PowerShell

```powershell
./scripts/run.ps1 1
./scripts/run.ps1 2
./scripts/run.ps1 3
./scripts/run.ps1 4
```

### Bash

```bash
bash scripts/run.sh 1
bash scripts/run.sh 2
bash scripts/run.sh 3
bash scripts/run.sh 4
```

Both scripts load `.env` then call `dotnet run --project ...`. The variable values never appear in app logs.

## A useful first prompt

`What is our VPN policy, and what is the status of ticket INC-1042?`

Notice the differences: app 01 has no knowledge source and should avoid pretending otherwise; app 02 can answer ticket status **from a mocked tool**; app 03 is a simple **declarative agent without knowledge/tools**; app 04 coordinates **specialized code-defined agents**.

## Common failures

- **Credential unavailable / authentication failed:** run `az login`, check the tenant and inspect which identity `DefaultAzureCredential` selects. No API key is needed.
- **401 / 403:** wrong tenant, role, token audience, resource, or expired/revoked sign-in.
- **404:** deployment name, endpoint route, or model doesn't support the API in that region.
- **400 on tool loop:** model lacks function-calling support or tools are incorrectly shaped; inspect the JSON trace.
- **Agent creation unauthorized:** additional management permission needed.
- **NuGet restore failure:** network/proxy, package versions or feed access; do not silently upgrade everything without noting API changes.
- **.NET build error:** check exact referenced package versions, then compare current official samples in SOURCES.md.

# Documentation and source links (checked 2026-11-16)

- Azure OpenAI Responses API request format and .NET options: https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/responses
- .NET local developer authentication with `DefaultAzureCredential` and `az login`: https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/local-development-dev-accounts (checked 2026-11-16)
- Select a `DefaultAzureCredential` chain member with `AZURE_TOKEN_CREDENTIALS`: https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/credential-chains (checked 2026-11-16)
- `DefaultAzureCredential` for .NET: https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential (checked 2026-11-16)
- Azure OpenAI resource endpoint and Entra token scope: https://learn.microsoft.com/en-us/azure/developer/ai/how-to/switching-endpoints (checked 2026-11-16)
- Responses model capabilities and reasoning: https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/reasoning (checked 2026-11-16)
- Strict function calling and parallel-call limitation: https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/structured-outputs (checked 2026-11-16)
- Microsoft Foundry / Agent Framework .NET quickstart: https://github.com/microsoft/agent-framework/blob/main/README.md
- Foundry persisted agents inside .NET workflows: https://learn.microsoft.com/en-us/agent-framework/workflows/agents-in-workflows
- Microsoft Agent Framework .NET project conventions: https://github.com/microsoft/agent-framework/blob/main/dotnet/samples/AGENTS.md
- Foundry C# hosted agents and samples: https://github.com/microsoft-foundry/foundry-samples/tree/main/samples/csharp/hosted-agents
- NuGet versions: https://www.nuget.org/packages/Azure.AI.Projects and https://www.nuget.org/packages/Microsoft.Agents.AI.Workflows
- Microsoft.Agents.AI.Foundry 1.28.0 preview package: https://www.nuget.org/packages/Microsoft.Agents.AI.Foundry/1.28.0-preview.261106.1

This kit intentionally pins a package family rather than using floating `*` versions. The Microsoft Agent Framework package family moves rapidly, and up-to-date documentation can refer to a later version than the pin. If Copilot updates the code, require a `dotnet build` and mention the versions changed.

**Authentication change (2026-11-16):** all four apps now use `DefaultAzureCredential`. Apps 01–02 no longer accept `AZURE_OPENAI_API_KEY` and acquire Entra bearer tokens before every REST request so the credential can refresh them. Apps 03–04 pass the same credential type to `AIProjectClient` instead of `AzureCliCredential`. Local development uses `az login`; another configured identity can be selected earlier in the default chain. Existing key-only setups must switch to an Entra identity with the required RBAC permissions. No package versions or service routes changed.

**Agent Framework package update (2026-11-16):** apps 03–04 pin `Microsoft.Agents.AI.Foundry` to `1.28.0-preview.261106.1`, because the configured package feed publishes this preview but not stable `1.28.0`. That preview requires `Azure.AI.Projects` `3.0.0-beta.5`; its `Azure.Core` 1.63 dependency already supplies `DefaultAzureCredential`, so the direct `Azure.Identity` reference was removed from apps 03–04 to avoid duplicate credential type definitions. Apps 01–02 retain `Azure.Identity` 1.17.0 because they do not depend on Azure.Core 1.63. These are prerelease SDK dependencies, so future upgrades may require API migration; the current code compiled without source API changes. Review the [Agent Framework workflow guidance](https://learn.microsoft.com/en-us/agent-framework/workflows/agents-in-workflows) and [`DefaultAzureCredential` API reference](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential) when changing them.

**Model configuration (2026-11-16):** `.env.example` recommends GPT-5.4-mini; the actual deployment name remains configurable. It also selects `AzureCliCredential` inside `DefaultAzureCredential` so the local samples use the account from `az login` instead of another developer credential. App 02 now sets `parallel_tool_calls=false` for its strict function schema, limiting each model response to at most one call. This is an intentional behavioral change from the previous request's default parallel-call setting; application code still validates and executes the requested function.

**Build/runtime status (2026-11-16):** restore and build succeeded with .NET 10. Live tests with a configured GPT-5.4-mini deployment covered two linked responses, known and unknown fictional ticket lookups, managed agent creation/invocation/deletion, and all three workflow stages. Model output and regional availability can vary.

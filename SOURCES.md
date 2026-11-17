# Documentation and source links (checked 2026-11-17)

- Azure OpenAI Responses API request format and .NET options: https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/responses
- .NET local developer authentication with `DefaultAzureCredential` and `az login`: https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/local-development-dev-accounts (checked 2026-11-16)
- Select a `DefaultAzureCredential` chain member with `AZURE_TOKEN_CREDENTIALS`: https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/credential-chains (checked 2026-11-16)
- `DefaultAzureCredential` for .NET: https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential (checked 2026-11-16)
- Azure OpenAI resource endpoint and Entra token scope: https://learn.microsoft.com/en-us/azure/developer/ai/how-to/switching-endpoints (checked 2026-11-16)
- Responses model capabilities and reasoning: https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/reasoning (checked 2026-11-16)
- Strict function calling and parallel-call limitation: https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/structured-outputs (checked 2026-11-16)
- Microsoft Foundry / Agent Framework .NET quickstart: https://github.com/microsoft/agent-framework/blob/main/README.md
- Foundry persisted agents inside .NET workflows: https://learn.microsoft.com/en-us/agent-framework/workflows/agents-in-workflows
- Agent Framework workflow visualization and .NET `ToMermaidString()`: https://learn.microsoft.com/en-us/agent-framework/workflows/visualization (checked 2026-11-16)
- Microsoft Agent Framework .NET project conventions: https://github.com/microsoft/agent-framework/blob/main/dotnet/samples/AGENTS.md
- Foundry C# hosted agents and samples: https://github.com/microsoft-foundry/foundry-samples/tree/main/samples/csharp/hosted-agents
- NuGet versions: https://www.nuget.org/packages/Azure.AI.Projects and https://www.nuget.org/packages/Microsoft.Agents.AI.Workflows
- Microsoft.Agents.AI.Foundry 1.28.0 preview package: https://www.nuget.org/packages/Microsoft.Agents.AI.Foundry/1.28.0-preview.261106.1
- Official MCP Inspector and current installation requirements: https://github.com/modelcontextprotocol/inspector (checked 2026-11-16; demo uses 2.14.0)
- Inspector v2 server configuration and `--config` versus `--catalog`: https://github.com/modelcontextprotocol/inspector/blob/main/docs/mcp-server-configuration.md
- Inspector v1 to v2 migration: https://github.com/modelcontextprotocol/inspector/blob/main/docs/v1-to-v2-migration.md
- Microsoft Learn MCP Server overview, endpoint, and authentication requirements: https://learn.microsoft.com/en-us/training/support/mcp (checked 2026-11-16)
- Microsoft Learn MCP developer reference and tool discovery: https://learn.microsoft.com/en-us/training/support/mcp-developer-reference (checked 2026-11-16)
- DeepWiki public, read-only MCP server and Streamable HTTP endpoint: https://docs.devin.ai/work-with-devin/deepwiki-mcp (checked 2026-11-16)
- Agent Framework C# MCP client discovery, `McpClientTool`/`AITool` registration and agent execution: https://learn.microsoft.com/en-us/agent-framework/agents/tools/local-mcp-tools?pivots=programming-language-csharp (checked 2026-11-17)
- .NET MCP client quickstart and client/runtime function invocation boundary: https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/build-mcp-client (checked 2026-11-17)
- Microsoft Learn Streamable HTTP transport and `DelegatingAIFunction` examples: https://learn.microsoft.com/en-us/microsoftteams/platform/agents-in-teams/integrate-ai/ai-agent-sample-walkthrough?pivots=teams-sdk-csharp (checked 2026-11-17)
- Official MCP C# SDK and releases: https://github.com/modelcontextprotocol/csharp-sdk and https://www.nuget.org/packages/ModelContextProtocol/2.4.0 (checked 2026-11-17)

This kit intentionally pins a package family rather than using floating `*` versions. The Microsoft Agent Framework package family moves rapidly, and up-to-date documentation can refer to a later version than the pin. If Copilot updates the code, require a `dotnet build` and mention the versions changed.

**Authentication change (2026-11-16):** all four apps now use `DefaultAzureCredential`. Apps 01–02 no longer accept `AZURE_OPENAI_API_KEY` and acquire Entra bearer tokens before every REST request so the credential can refresh them. Apps 03–04 pass the same credential type to `AIProjectClient` instead of `AzureCliCredential`. Local development uses `az login`; another configured identity can be selected earlier in the default chain. Existing key-only setups must switch to an Entra identity with the required RBAC permissions. No package versions or service routes changed.

**Agent Framework package update (2026-11-16):** apps 03–04 pin `Microsoft.Agents.AI.Foundry` to `1.28.0-preview.261106.1`, because the configured package feed publishes this preview but not stable `1.28.0`. That preview requires `Azure.AI.Projects` `3.0.0-beta.5`; its `Azure.Core` 1.63 dependency already supplies `DefaultAzureCredential`, so the direct `Azure.Identity` reference was removed from apps 03–04 to avoid duplicate credential type definitions. Apps 01–02 retain `Azure.Identity` 1.17.0 because they do not depend on Azure.Core 1.63. These are prerelease SDK dependencies, so future upgrades may require API migration; the current code compiled without source API changes. Review the [Agent Framework workflow guidance](https://learn.microsoft.com/en-us/agent-framework/workflows/agents-in-workflows) and [`DefaultAzureCredential` API reference](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential) when changing them.

**Model configuration (2026-11-16):** `.env.example` recommends GPT-5.4-mini; the actual deployment name remains configurable. It also selects `AzureCliCredential` inside `DefaultAzureCredential` so the local samples use the account from `az login` instead of another developer credential. App 02 now sets `parallel_tool_calls=false` for its strict function schema, limiting each model response to at most one call. This is an intentional behavioral change from the previous request's default parallel-call setting; application code still validates and executes the requested function.

**Build/runtime status (2026-11-16):** restore and build succeeded with .NET 10. Live tests with a configured GPT-5.4-mini deployment covered two linked responses, known and unknown fictional ticket lookups, managed agent creation/invocation/deletion, and all three workflow stages. Model output and regional availability can vary.

**MCP sample addition (2026-11-17, issue #3):** app 05 adds `ModelContextProtocol`
`2.4.0` and reuses the app 04 pins for `Azure.AI.Projects` `3.0.0-beta.5` and
`Microsoft.Agents.AI.Foundry` `1.28.0-preview.261106.1`. No existing app package or
API surface changed; no breaking migration is required. The MCP SDK owns
Streamable HTTP initialization, discovery and remote calls. Its discovered
`McpClientTool` already derives from `AIFunction`; a delegating wrapper preserves
that schema while exposing dispatch, tool-error checks and a call budget.
The [Agent Framework MCP guidance](https://learn.microsoft.com/en-us/agent-framework/agents/tools/local-mcp-tools?pivots=programming-language-csharp)
shows stdio transport; app 05 instead uses `HttpClientTransport` with
`HttpTransportMode.StreamableHttp` for Learn's
[documented public endpoint](https://learn.microsoft.com/en-us/training/support/mcp-developer-reference).
No Azure credentials are supplied to Learn.

**MCP build/runtime status (2026-11-17):** solution restore/build succeeded.
Live discovery and direct search succeeded in `--tools-only` mode without an
Azure client. Agent mode also succeeded against the configured Foundry model:
the runtime dispatched one discovered `microsoft_docs_search` call through MCP
before returning an answer. These are live observations, not guarantees of
future tool availability, schemas, model behavior or citation correctness.
Offline checks in `tests\McpClientAgent.Tests` also passed for schema-preserving
dispatch, the three-call budget, `isError=true`, and an SDK protocol exception
after a previously successful call. The HTTP fixture opens no network sockets.

## Foundry IQ hotel RAG sample (app 06, issue #5; checked 2026-11-17)

- Foundry IQ workflow and reusable knowledge bases:
  https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/what-is-foundry-iq
- Direct file knowledge sources, minimal PDF extraction, synchronous ingestion,
  filename retention, upload limits and duplicate-file semantics:
  https://learn.microsoft.com/en-us/azure/search/agentic-knowledge-source-how-to-file
- File knowledge source creation contract:
  https://learn.microsoft.com/en-us/rest/api/searchservice/knowledge-sources/create-or-update?view=rest-searchservice-2026-08-01-preview
- Direct file upload contract:
  https://learn.microsoft.com/en-us/rest/api/searchservice/knowledge-sources/upload-file?view=rest-searchservice-2026-08-01-preview
- Knowledge base creation, optional planning model and minimal extractive retrieval:
  https://learn.microsoft.com/en-us/azure/search/agentic-retrieval-how-to-create-knowledge-base
- Agent Service / Foundry IQ MCP integration, allowlisted
  `knowledge_base_retrieve`, project-managed-identity connection and RBAC:
  https://learn.microsoft.com/en-us/azure/foundry/agents/how-to/foundry-iq-connect
- Managed MCP tool execution in .NET:
  https://learn.microsoft.com/en-us/azure/foundry/agents/how-to/tools/model-context-protocol?pivots=csharp
- Search service provisioning options:
  https://learn.microsoft.com/en-us/cli/azure/search/service?view=azure-cli-latest#az-search-service-create
- Keyless Search roles:
  https://learn.microsoft.com/en-us/azure/search/search-security-rbac
- Search pricing and ongoing costs:
  https://learn.microsoft.com/en-us/azure/search/search-sku-manage-costs

**Packages/API compatibility:** app 06 pins `Azure.AI.Projects` and
`Azure.AI.Extensions.OpenAI` to `3.0.0-beta.5`, matching the Projects family already
used by the other managed-agent samples. The explicit Extensions pin avoids
NuGet selecting an older beta for Projects' unavailable alpha dependency floor.
No existing app's package version or API behavior changes; no breaking migration
is required. App 06 needs neither Agent Framework nor the MCP client SDK:
Foundry Agent Service owns the remote MCP session.

Search knowledge and MCP endpoints use `2026-08-01-preview`, project management
uses `2025-06-01`, and project connections use `2025-10-01-preview`. The linked
IQ guide's C# snippet still shows an older Search version and
`GlobalMcpToolCallApprovalPolicy`; the pinned .NET SDK instead exposes
`DefaultMcpToolCallApprovalPolicy`. App 06 uses the latter and the current Search
API used in that guide's REST instructions. Compile against the pins rather than
mixing similarly named types from different preview generations.

**Teaching choice:** a direct `file` knowledge source is real Foundry IQ, not
OpenAI `file_search`. It avoids Blob Storage/indexer provisioning for three
small PDFs. The source performs extraction/chunking/embedding; the knowledge
base returns extractive grounding data without an additional planning/synthesis
model. Only the managed travel agent generates the final recommendation.
Prompts require source attribution and abstention; observed MCP use and successful
execution are checked separately from human verification of citation accuracy.

**Build/runtime status (2026-11-17):** six-project solution restore/build and
offline evidence-gate checks passed, including absent retrieval, unexpected
output shape, MCP errors, allowlist violations and partial/incomplete responses.
A dedicated keyless Basic Search service was provisioned in `ai-130-demo`.
Live retrieval recommended Thameside House (London river views/breakfast),
Isar Garden Hotel (Munich river/spa), and Le Jardin Secret (Paris courtyard),
each citing its corresponding uploaded PDF. A separate price/availability
question explicitly abstained. All four temporary agents were deleted.
Setup reruns skipped existing uploads and the service contained exactly three
error-free brochure records. The initial upload returned a transient Azure
cancellation; a rerun succeeded. Setup now bounds transient HTTP upload retries
and checks for an already-completed upload before retrying. These are observed
results, not guarantees about future service availability or model accuracy.

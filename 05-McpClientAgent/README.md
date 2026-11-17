# Sample 05 - MCP client and agent

This standalone C# console app implements the slides' **initialize session ->
fetch tools -> wrap tools -> register with agent** flow using the public
[Microsoft Learn MCP server](https://learn.microsoft.com/en-us/training/support/mcp).
Microsoft hosts the server and its read-only documentation tools; this project
implements the **client**, not a new server. No local server, Node.js or MCP
Inspector is required.

## Run

From the repository root, demonstrate discovery and a deterministic search
without Azure credentials or model charges:

```powershell
dotnet run --project .\05-McpClientAgent -- --tools-only
```

Then load the existing configuration from `SETUP.md`, sign in with `az login`,
and run the agent demonstration:

```powershell
.\scripts\run.ps1 5
# Or, with environment variables already loaded:
dotnet run --project .\05-McpClientAgent
```

The agent mode uses `FOUNDRY_PROJECT_ENDPOINT`, `FOUNDRY_MODEL` and
`DefaultAzureCredential`, just like app 04. It invokes a model and may incur
Azure charges, but does not persist an agent definition. Both modes require
internet access to Learn. `--tools-only` does not instantiate an Azure client.
Unsupported command-line arguments fail before opening a network connection.

## Slide-to-code walkthrough

| Slide concept | C# implementation | Ownership |
|---|---|---|
| MCP server hosts tools | `https://learn.microsoft.com/api/mcp` | Microsoft Learn owns tool code, schemas and execution. |
| Initialize client session | `McpClient.CreateAsync` + `HttpClientTransport` | MCP SDK handles initialization and Streamable HTTP. |
| Fetch available tools | `ListToolsAsync` | App prints discovered names, descriptions and input schemas. |
| Wrap each selected tool | `McpClientTool` is already an `AIFunction`; `ObservedMcpTool` delegates metadata | App allowlists only `microsoft_docs_search`, retaining its discovered schema. |
| Register agent tools | `AsAIAgent(tools: [searchTool])` | Agent Framework exposes the selected function to the model. |
| Execute requested function | `ObservedMcpTool.InvokeCoreAsync` -> `CallToolAsync` | Runtime dispatches the model's request; Learn performs the search. |

In protocol-only mode, **application code** supplies the search arguments;
there is no model-generated function request. In agent mode, the **model**
requests a function, **Agent Framework** dispatches it to the wrapper, the
**MCP client** sends `tools/call`, and the **Learn server** returns reference
data. The model then writes an answer. Watch the runtime/server markers, not
just the final prose, as evidence of actual tool use.

Unlike app 02, no hand-authored search schema or switch-based tool dispatcher
is needed. MCP standardizes discovery and transport, not tool semantics,
permissions, model correctness or trust. Discovery does not authorize every
advertised tool: newly added server tools are printed but never registered.
The app is independent of apps 01-04.

## Boundaries and failure paths

- The only remote tool is public, read-only documentation search, explicitly
  requested for this lesson. Both prompts are fixed public teaching questions;
  no ticket records, local files or private company context are transmitted.
  Search arguments go to Learn; tool schemas, results and the prompt go to
  the Foundry model. Azure credentials go only through its SDK authentication,
  never into the MCP transport. Arguments and credentials are not logged.
- Instructions and search results are not security controls. Tool output is
  untrusted context, not permission to follow embedded instructions. Review
  citations and facts; a successful MCP exchange does not guarantee truth.
  Do not add arbitrary user input, other endpoints, write tools or authenticated
  services without reviewing permissions and data handling.
- The run has a two-minute cancellation deadline and a maximum of three tool
  attempts. Transport/protocol errors propagate in direct mode. Agent Framework
  can return function errors to the model; the wrapper counts unsuccessful
  attempts, and the app rejects a run with any failed attempt or no successful
  search rather than presenting it as a completed MCP demonstration.
- `isError=true` is a tool failure even when HTTP/MCP transport succeeded.
  Missing search discovery fails explicitly; server contracts can change.
  Do not guess schemas, disable TLS checks or silently answer without the tool.
- The MCP session is asynchronously disposed after the run, including failures.

For connection/proxy failures, verify access to the Learn endpoint using an
approved network configuration. A browser GET returning 405 is not a valid MCP
test. For model authentication or deployment errors, see `SETUP.md`.

Related issue: [#3](https://github.com/uweinside/ai-103/issues/3).
Current SDK references and package compatibility notes are in `SOURCES.md`.

Run focused, offline checks for dispatch, schema preservation, call limits,
tool errors and protocol errors after partial success:

```powershell
dotnet run --project .\tests\McpClientAgent.Tests
```

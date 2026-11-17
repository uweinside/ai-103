# AI-103 — Five C# Console Teaching Apps

**Audience:** Microsoft technical trainers, who already understand LLM/agent architecture.  
**Teaching sequence:** direct model API → manual function-calling loop → Foundry-managed agent → Agent Framework multi-agent orchestration → MCP client/tool integration.

Open **`AI103-Teaching.code-workspace`** in VS Code. Each sample is independent. Apps 01–04 use a fictional **IT service desk**, with synthetic ticket data; app 05 searches public Microsoft Learn documentation. No production system is modified.

| Project | Demonstrates | Who owns the orchestration? | Day 2 connection |
|---|---|---|---|
| `01-DirectResponses` | REST Responses API, instructions, `previous_response_id` | Application + Responses service | APIs, model deployment, context |
| `02-ManualToolCalling` | JSON function schema, tool-call output, safe dispatch loop | **Your C# code** | Function calling, tool integration |
| `03-FoundryManagedAgent` | `AIProjectClient`, versioned declarative agent, `AsAIAgent` | Foundry service | Agent Service lifecycle |
| `04-AgentFrameworkWorkflow` | Code-defined `AIAgent`, concurrent fan-out/fan-in, collector, streaming and Mermaid summary | Agent Framework | Concurrent orchestration |
| `05-McpClientAgent` | Streamable HTTP MCP session, discovered schemas, allowlisted tool, agent dispatch | Application + Agent Framework + MCP client/server | MCP tool integration |

## Prerequisites

- .NET 10 SDK (recommended for this kit), VS Code with C# Dev Kit, Azure CLI.
- A **Microsoft Foundry project** and a deployed text model that supports the **Responses API** (01–02) and agent use (03–05). App 05's `--tools-only` mode needs no Azure access.
- `az login` in the correct tenant, plus project/model permissions. All five apps use `DefaultAzureCredential` for model access with Microsoft Entra ID, never API keys. See [SETUP.md](SETUP.md).
- Internet access for NuGet restore and Azure APIs. Azure usage may incur charges.
- Apps 03–05 currently pin prerelease Foundry/Projects packages to match the available feed. See [SOURCES.md](SOURCES.md) for the versions and upgrade caveat.

## Quick start

1. Open `AI103-Teaching.code-workspace` in VS Code.
2. Run `az login` and verify subscription/tenant.
3. Configure the environment variables as explained in `SETUP.md`. If using `.env`, load it into the terminal before running a project directly, or use the supplied launcher script.
4. In the VS Code integrated terminal, run one sample:

```bash
dotnet restore 01-DirectResponses/DirectResponses.csproj
dotnet run --project 01-DirectResponses
```

Then proceed to the next project. **These are intentionally five separate applications, not a chain with shared compiled dependencies.**

For the in-code MCP demonstration, run `dotnet run --project .\05-McpClientAgent -- --tools-only` without Azure, then `.\scripts\run.ps1 5` for the agent version. See [sample 05](05-McpClientAgent/README.md).

For the optional MCP protocol demonstration, see [Install and run MCP Inspector](MCP-INSPECTOR.md), including how to restart it after rebooting.

## Trainer demo progression

1. Start with a query about VPN policy, then ask a follow-up. Inspect request JSON and the `previous_response_id` value. Explain that state still has a scope, a retention policy, and a cost.
2. Ask "What's ticket INC-1042's status?" Inspect the model's JSON tool request. Pause before dispatch; show that the model **does not execute your .NET method**. Your code must validate and execute it. Then submit the result back as `function_call_output`.
3. Create a named, versioned agent definition in Foundry. Compare its persistent definition with a model request. Query it, then examine its entry in the portal. The sample can delete its created version automatically.
4. Run technical and incident specialists concurrently on the same fictional VPN question. Show the explicit fan-out and fan-in barrier, watch attributed stream updates, and inspect how the composer reconciles both findings. Copy the final text Mermaid diagram; contrast this topology with the sequential course exercise.
5. Connect an MCP client to Microsoft Learn, discover tools and inspect their schemas. Contrast a direct application-selected search (`--tools-only`) with a model-requested search dispatched by Agent Framework. The client sends the request; the remote server executes the tool.

### Scope and what is **not** implemented

- No real VPN changes, ticket writes, custom MCP server hosting, Foundry IQ knowledge base, Teams publication, or production hosting. App 05 connects to the real, public, read-only Microsoft Learn MCP server. No secrets are embedded in source code.
- App 01 uses the Responses REST contract for maximal visibility. Apps 03–05 use Microsoft SDK abstractions. The contrast is intentional.
- Check current build and live-test status, package versions, and documentation links in [SOURCES.md](SOURCES.md).

## Where to continue with GitHub Copilot

Start with [.github/copilot-instructions.md](.github/copilot-instructions.md) and [COPILOT-CHALLENGES.md](COPILOT-CHALLENGES.md). The exercises are designed to add concepts **without hiding the architecture**.

## Original Microsoft AI-103 course connections

- **Day 1 slides 18–26**: model endpoints, SDKs, Responses / Chat Completions and chat application; map to app 01.
- **Day 1 slides 27–38** and **Day 2 slides 13–17**: tool calling and custom tools; map to app 02.
- **Day 2 slides 4–12**: Agent Service and agent lifecycle (but course notes explicitly mark the first section redundant in the full two-day format); map to app 03.
- **Day 2 slides 42–55**: Agent Framework and orchestration; map to app 04.
- **Day 2 slides 18–22 (MCP)**: app 05 maps MCP session initialization, discovery, tool wrapping and registration to C#.
- **Day 2 slides 23–30 (Foundry IQ), 31–35 (M365 integration)**: later extensions; see challenges. Not falsely presented as implemented.

**Educational safety:** all ticket records are fabricated; never attach real secrets or production write tools without authorization, input validation, confirmation and access controls.

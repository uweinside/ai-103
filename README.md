# AI-103 — Four C# Console Teaching Apps

**Audience:** Microsoft technical trainers, who already understand LLM/agent architecture.  
**Teaching sequence:** direct model API → manual function-calling loop → Foundry-managed agent → Agent Framework multi-agent orchestration.

Open **`AI103-Teaching.code-workspace`** in VS Code. Each sample is independent. The examples use a fictional **IT service desk**, with synthetic ticket data; no production system is modified.

| Project | Demonstrates | Who owns the orchestration? | Day 2 connection |
|---|---|---|---|
| `01-DirectResponses` | REST Responses API, instructions, `previous_response_id` | Application + Responses service | APIs, model deployment, context |
| `02-ManualToolCalling` | JSON function schema, tool-call output, safe dispatch loop | **Your C# code** | Function calling, tool integration |
| `03-FoundryManagedAgent` | `AIProjectClient`, versioned declarative agent, `AsAIAgent` | Foundry service | Agent Service lifecycle |
| `04-AgentFrameworkWorkflow` | Code-defined `AIAgent`, `WorkflowBuilder`, edges, streaming | Agent Framework | Orchestration and workflows |

## Prerequisites

- .NET 10 SDK (recommended for this kit), VS Code with C# Dev Kit, Azure CLI.
- A **Microsoft Foundry project** and a deployed text model that supports the **Responses API** (01–02) and agent use (03–04).
- `az login` in the correct tenant, plus project/model permissions. All four apps use `DefaultAzureCredential` with Microsoft Entra ID, never API keys. See [SETUP.md](SETUP.md).
- Internet access for NuGet restore and Azure APIs. Azure usage may incur charges.
- Apps 03–04 currently pin prerelease Foundry/Projects packages to match the available feed. See [SOURCES.md](SOURCES.md) for the versions and upgrade caveat.

## Quick start

1. Open `AI103-Teaching.code-workspace` in VS Code.
2. Run `az login` and verify subscription/tenant.
3. Set environment variables as explained in `SETUP.md` (or use `.env` with the supplied launcher script).
4. In the VS Code integrated terminal, run one sample:

```bash
dotnet restore 01-DirectResponses/DirectResponses.csproj
dotnet run --project 01-DirectResponses
```

Then proceed to the next project. **These are intentionally four separate applications, not a chain with shared compiled dependencies.**

## Trainer demo progression

1. Start with a query about VPN policy, then ask a follow-up. Inspect request JSON and the `previous_response_id` value. Explain that state still has a scope, a retention policy, and a cost.
2. Ask "What's ticket INC-1042's status?" Inspect the model's JSON tool request. Pause before dispatch; show that the model **does not execute your .NET method**. Your code must validate and execute it. Then submit the result back as `function_call_output`.
3. Create a named, versioned agent definition in Foundry. Compare its persistent definition with a model request. Query it, then examine its entry in the portal. The sample can delete its created version automatically.
4. Compare two specialized agents. Build a sequential workflow, show the explicit edges and watch events from each stage.

### Scope and what is **not** implemented

- No real VPN changes, ticket writes, real MCP server, Foundry IQ knowledge base, Teams publication, or production hosting. Those are intended Copilot extensions. No secrets are embedded in source code.
- App 01 uses the Responses REST contract for maximal visibility. Apps 03–04 use Microsoft SDK abstractions. The contrast is intentional.
- **Not executed against a real Azure tenant in the generation environment.** Check current build and live-test status, package versions, and documentation links in [SOURCES.md](SOURCES.md).

## Where to continue with GitHub Copilot

Start with [.github/copilot-instructions.md](.github/copilot-instructions.md) and [COPILOT-CHALLENGES.md](COPILOT-CHALLENGES.md). The exercises are designed to add concepts **without hiding the architecture**.

## Original Microsoft AI-103 course connections

- **Day 1 slides 18–26**: model endpoints, SDKs, Responses / Chat Completions and chat application; map to app 01.
- **Day 1 slides 27–38** and **Day 2 slides 13–17**: tool calling and custom tools; map to app 02.
- **Day 2 slides 4–12**: Agent Service and agent lifecycle (but course notes explicitly mark the first section redundant in the full two-day format); map to app 03.
- **Day 2 slides 42–55**: Agent Framework and orchestration; map to app 04.
- **Day 2 slides 18–22 (MCP), 23–30 (Foundry IQ), 31–35 (M365 integration)**: later extensions; see challenges. Not falsely presented as implemented.

**Educational safety:** all ticket records are fabricated; never attach real secrets or production write tools without authorization, input validation, confirmation and access controls.

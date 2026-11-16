# AI-103 Instructor Training Samples — Copilot guidance

We are teaching Microsoft Foundry developer concepts **progressively**, not constructing a production help desk. Preserve the educational contrast:

1. App 01: explicit Responses REST calls and `previous_response_id`.
2. App 02: model returns a **function-call request**, and user code explicitly validates/dispatches it. Never change the sample to imply the LLM invoked code by itself.
3. App 03: persisted, versioned Foundry Agent Service definition, managed using `AIProjectClient`.
4. App 04: **code-defined** `AIAgent` abstractions plus an explicit Agent Framework workflow topology.

Requirements for every code change:
- Write detailed, precise teaching comments explaining **why**, ownership boundaries, model limitations, and error paths.
- Keep sample data fictional, local and read-only. Ask before adding network tools, write operations, or identity-sensitive functionality.
- Never introduce hard-coded keys or log credentials. Preserve `.env` and Azure CLI patterns.
- If changing packages or API surfaces, cite the up-to-date Microsoft Learn link in `SOURCES.md` and explain the breaking change.
- Keep programs standalone, as simple as possible, and run `dotnet restore`, `dotnet build`, then a live test if Azure access is configured.
- Keep all four apps independent. Prefer small additions over a shared abstraction framework.

Suggested next tasks: add an MCP server to app 02 or 04, connect real Foundry IQ to app 03, add human approval gates to app 04, or add OpenTelemetry traces and an evaluation case.

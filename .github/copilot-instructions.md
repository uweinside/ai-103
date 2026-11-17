# AI-103 Instructor Training Samples — Copilot guidance

We are teaching Microsoft Foundry developer concepts **progressively**, not constructing a production help desk. Preserve the educational contrast:

1. App 01: explicit Responses REST calls and `previous_response_id`.
2. App 02: model returns a **function-call request**, and user code explicitly validates/dispatches it. Never change the sample to imply the LLM invoked code by itself.
3. App 03: persisted, versioned Foundry Agent Service definition, managed using `AIProjectClient`.
4. App 04: **code-defined** `AIAgent` abstractions plus an explicit Agent Framework workflow topology.
5. App 05: **application-owned MCP client**, runtime tool discovery and allowlisted remote search registered with a code-defined agent. The model requests; the runtime dispatches; the Learn server executes. `--tools-only` has no model.
6. App 06: **Foundry IQ RAG** over the three fictional travel brochures. Agent Service dispatches the allowlisted knowledge-base MCP tool server-side; the console does not run an MCP client or pretend local PDF search is IQ.

Requirements for every code change:
- Write detailed, precise teaching comments explaining **why**, ownership boundaries, model limitations, and error paths.
- Keep sample data fictional, local and read-only. Ask before adding network tools, write operations, or identity-sensitive functionality.
- Never introduce hard-coded keys or log credentials. Preserve `.env` and Azure CLI patterns.
- If changing packages or API surfaces, cite the up-to-date Microsoft Learn link in `SOURCES.md` and explain the breaking change.
- Keep programs standalone, as simple as possible, and run `dotnet restore`, `dotnet build`, then a live test if Azure access is configured.
- Keep all six apps independent. Prefer small additions over a shared abstraction framework.

App 05's public, read-only Microsoft Learn MCP connection is explicitly authorized for its fixed public teaching queries; do not extend this to arbitrary endpoints or private data without asking.

App 06's setup is authorized to upload the three `etc/collateral` PDFs and create
sample-owned Foundry IQ resources, including a dedicated Search service in the
configured Foundry account's resource group (`ai-130-demo` for this environment).
Do not use the existing Search services in `rg-modelworld`. Keep ingestion and
resource writes in the explicit setup script; runtime hotel retrieval is read-only
apart from its uniquely named temporary agent's creation/deletion. No bookings.

Suggested next tasks: contrast app 05's MCP client with app 02's local dispatch, connect real Foundry IQ to app 03, add human approval gates to app 04, or add OpenTelemetry traces and an evaluation case.

# Sample 04 — Agent Framework workflow

## Teaching goals

- Show three agent roles sharing the same deployed model.
- Point to WorkflowBuilder edges as explicit orchestration.
- Watch stream events and executor IDs as each agent runs.
- Copy a Mermaid run-summary diagram showing each agent's role and observed output preview.
- Challenge: replace sequential graph with a controlled handoff or approval step.

## Run

From repository root:

```bash
dotnet run --project 04-AgentFrameworkWorkflow
```

See root `SETUP.md` for environment variables and root `SOURCES.md` for current Microsoft documentation. Source code includes intentionally extensive teaching comments.

## Mermaid run summary

After a successful run, copy the lines **inside** the final `mermaid` code fence into
[Mermaid Live](https://mermaid.live). The diagram uses short agent names:
**Triage -> Review -> Reply**, with each role and its observed text output in its node.

Previews collapse whitespace and contain at most 240 Unicode text elements,
including `...` when truncated. Truncation prefers a word boundary; a long
unbroken word is cut without splitting emoji or combining accents. Previews
are excerpts, not model-generated summaries; consult the full streamed output
for omitted context. Model text is escaped to prevent it becoming diagram syntax
or HTML. `(no text output observed)` means no text was captured, not that an
agent completed successfully.

The ordered stages configure both workflow edges and diagram edges. Arrows show
the **configured sequential flow**, while previews come from this run's streamed
executor events. This is not a timing/completion trace and does not claim any
real VPN action was performed. Errors propagate instead of printing a
completed-run diagram. No additional model calls, packages, or network tools
are needed.

The framework's `ToMermaidString()` exports topology alone. This sample uses a
small local renderer to include roles and runtime output safely, without editing
SDK-generated Mermaid text.

Run the dependency-free preview and escaping checks without Azure:

```powershell
dotnet run --project .\tests\WorkflowDiagram.Tests
```

## Instructor prompt

`My VPN is failing and I want to know the status of INC-1042.`

**Important:** This is an educational demonstration, not a complete enterprise help-desk application. No real ticket or VPN system is connected.

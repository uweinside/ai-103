# Sample 04 — Concurrent Agent Framework workflow

## Why are there two specialists?

There are actually **three agents**: two specialists work independently in
parallel, then a third agent, **Compose**, combines their findings into one
answer. Think of a service desk consulting a technical colleague and an incident
coordinator before replying to a user.

| Agent | Its question | What it knows in this sample |
|---|---|---|
| **Technical** | "What could cause VPN error 809, and what is safe to observe?" | Only fictional `TECH-809` guidance: gateway reachability or a local network issue; no incident records |
| **Incident** | "Does the recorded outage match this user's symptoms?" | Only the fictional `INC-1042` snapshot: Northstar VPN, Europe, multiple error-809 reports; no live status or confirmed per-user diagnosis |
| **Compose** | "What should we tell the user after considering both findings?" | The original question and both attributed specialist responses |

For the default question, the user mentions a password change. **Technical**
should explain that the timing does not prove a password problem and suggest
safe observations. **Incident** contributes a more specific lead: the snapshot
describes a matching shared outage. **Compose** should prioritize that evidence
without claiming it proves this user's root cause, inventing an ETA, or describing
the snapshot as live status.

The intended progression is:

```text
Technical: Error 809 has several possible causes; do not jump to a diagnosis.
Incident:  The fictional snapshot records a matching shared outage.
Compose:   The outage is a stronger lead, but this user's cause is unconfirmed;
           retain safe observations and acknowledge the snapshot's limitations.
```

These are illustrative summaries, not fixed model output. The specialists do
not read each other's answers, and Compose is not a third vote: it reconciles
different evidence. Waiting for both matters because either finding alone gives
an incomplete picture.

**Are multiple agents required? No.** One agent given both small fixtures could
handle this task more simply and potentially more cheaply. This sample separates
the roles to teach **fan-out, independent contexts, a join barrier, and final
composition**, not to claim that more agents always produce better answers.
All three use the same deployment and identity; no separate tools, data systems,
or security boundaries are created. More agents also add calls, cost, and failure
paths. A larger application should justify the split with real independent work,
not role names alone.

For a step-by-step explanation and tradeoffs, see
[demo 04 in the complete guide](../DEMO-GUIDE.md#why-split-the-work-between-two-specialists).

## Teaching goals

- Show three agent roles sharing the same deployed model.
- Point to WorkflowBuilder fan-out and fan-in barrier edges as explicit orchestration.
- Run two independent specialists on the same request; compose only after both finish.
- Reconcile a tentative local diagnosis with stronger fictional outage evidence.
- Watch stream events and executor IDs as each agent runs.
- Copy a Mermaid run-summary diagram showing each agent's role and observed output preview.
- Contrast concurrent orchestration (course slide 49) with sequential execution
  (slide 50). This is not group chat, handoff, or adaptive Magentic planning.
- Challenge: add a controlled handoff or approval step without hiding ownership.

```text
                  +--> Technical --+
User request -----+                +--> Compose --> Answer
                  +--> Incident ---+
```

Technical uses the fictional local `TECH-809` guidance. Incident uses the fixed
fictional `INC-1042` snapshot: an active Northstar VPN outage in Europe with no ETA.
These are prompt-embedded teaching fixtures, not tools, live lookups, or real
company policies. Both agents always run and never see each other's response.
Compose receives their attributed findings plus the original question, prioritizes
matching outage evidence over a speculative local cause, and preserves uncertainty
about the individual user's cause. Model adherence is not a deterministic guarantee.

## Run

From repository root:

```bash
dotnet run --project 04-AgentFrameworkWorkflow
```

With the repository `.env` loaded by the launcher:

```powershell
.\scripts\run.ps1 4
.\scripts\run.ps1 4 "How do I troubleshoot VPN error 809?"
```

The default request is: `My Northstar VPN in Europe fails with error 809 after a
password change. Is INC-1042 related?` An optional command-line question replaces it;
the same two specialists still run. Do not supply real tickets or personal data.

See root `SETUP.md` for environment variables and root `SOURCES.md` for current Microsoft documentation. Source code includes intentionally extensive teaching comments.

## What owns execution?

`ChatForwardingExecutor` broadcasts the question and a processing `TurnToken`;
`AddFanOutEdge` selects both branches without a model routing decision.
`AddFanInBarrierEdge` holds their outputs until both sources contribute.
The collector buffers the released messages for the delivery step, then invokes
the composer once. Agents share a deployment and credential, not a conversation.
Specialist bindings suppress forwarding their incoming user messages, avoiding
duplicate questions being treated as findings.

Streamed updates may interleave; completion order and response times can vary.
Executor/branch errors, missing findings, or an empty composer answer fail the run
explicitly. They do not mean "no incident found" and do not produce a completed-run
diagram. No production system is queried or modified.

## Mermaid run summary

After a successful run, copy the lines **inside** the final `mermaid` code fence into
[Mermaid Live](https://mermaid.live). The diagram uses short agent names:
**Technical**, **Incident**, and **Compose**, with each role and its observed text
output in its node. A deterministic input node forks to both specialists; each
specialist points to Compose. No arrow connects the specialists.

Previews collapse whitespace and contain at most 240 Unicode text elements,
including `...` when truncated. Truncation prefers a word boundary; a long
unbroken word is cut without splitting emoji or combining accents. Previews
are excerpts, not model-generated summaries; consult the full streamed output
for omitted context. Model text is escaped to prevent it becoming diagram syntax
or HTML. `(no text output observed)` means no text was captured, not that an
agent completed successfully.

The same specialist collection configures both workflow and diagram branches. Arrows show
the **configured concurrent flow**, while previews come from this run's streamed
executor events. This is not a timing/completion trace and does not claim any
real VPN action was performed. Errors propagate instead of printing a
completed-run diagram. No additional model calls, packages, or network tools
are needed.

The framework's `ToMermaidString()` exports topology alone. This sample uses a
small local renderer to include roles and runtime output safely, without editing
SDK-generated Mermaid text.

Run the offline preview, escaping, and real framework orchestration checks without Azure:

```powershell
dotnet run --project .\tests\WorkflowDiagram.Tests
```

The deterministic chat-client fixtures verify that both specialists start before
either finishes, the collector receives both attributed findings and runs once,
all three agents stream updates, and a failed or empty branch cannot yield a partial answer.
They use the app's pinned dependencies, not an external test framework.

## Instructor prompt

`My Northstar VPN in Europe fails with error 809 after a password change. Is INC-1042 related?`

Discuss why a plausible local explanation is not a confirmed diagnosis, why the
collector must wait, and why a static outage snapshot must not be described as live
status. Sequential agents build on previous results; concurrent agents contribute
independent perspectives that a collector must reconcile. The official sequential
exercise can remain the baseline; this sample demonstrates the next step.

**Important:** This is an educational demonstration, not a complete enterprise help-desk application. No real ticket or VPN system is connected.

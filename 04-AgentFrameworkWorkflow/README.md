# Sample 04 — Agent Framework workflow

## Teaching goals

- Show three agent roles sharing the same deployed model.
- Point to WorkflowBuilder edges as explicit orchestration.
- Watch stream events and executor IDs as each agent runs.
- Challenge: replace sequential graph with a controlled handoff or approval step.

## Run

From repository root:

```bash
dotnet run --project 04-AgentFrameworkWorkflow
```

See root `SETUP.md` for environment variables and root `SOURCES.md` for current Microsoft documentation. Source code includes intentionally extensive teaching comments.

## Instructor prompt

`My VPN is failing and I want to know the status of INC-1042.`

**Important:** This is an educational demonstration, not a complete enterprise help-desk application. No real ticket or VPN system is connected.

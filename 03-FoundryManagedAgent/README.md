# Sample 03 — Persisted Foundry agent

## Teaching goals

- Run with `KEEP_DEMO_AGENT=true` to retain the agent for portal inspection.
- Compare persisted definitions with model requests and code-defined agents.
- Inspect agent/version in Foundry; clean it up after the demonstration.
- Challenge: attach Foundry IQ knowledge and compare answers with the baseline.

## Run

From repository root:

```bash
dotnet run --project 03-FoundryManagedAgent
```

See root `SETUP.md` for environment variables and root `SOURCES.md` for current Microsoft documentation. Source code includes intentionally extensive teaching comments.

## Instructor prompt

`My VPN is failing and I want to know the status of INC-1042.`

**Important:** This is an educational demonstration, not a complete enterprise help-desk application. No real ticket or VPN system is connected.

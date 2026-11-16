# Sample 02 — Manual function calling

## Teaching goals

- Ask about `INC-1042`, then use a non-existent ticket number.
- Stop at the `calls` loop: the LLM made a request, not a function call into your process.
- Inspect strict JSON Schema, argument validation, allowlist and returned `function_call_output`.
- Ask what would change if the function could delete tickets.

## Run

From repository root:

```bash
dotnet run --project 02-ManualToolCalling
```

See root `SETUP.md` for environment variables and root `SOURCES.md` for current Microsoft documentation. Source code includes intentionally extensive teaching comments.

## Instructor prompt

`My VPN is failing and I want to know the status of INC-1042.`

**Important:** This is an educational demonstration, not a complete enterprise help-desk application. No real ticket or VPN system is connected.

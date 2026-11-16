# Sample 01 — Direct Responses API

## Teaching goals

- Set a breakpoint on `PostAsJsonAsync`. Inspect the JSON payload.
- Run two turns; compare response IDs and `previous_response_id`.
- Type `/new` and discuss model statelessness versus stored response state.
- Explain why a model cannot know the fictional company VPN policy.

## Run

From repository root:

```bash
dotnet run --project 01-DirectResponses
```

See root `SETUP.md` for environment variables and root `SOURCES.md` for current Microsoft documentation. Source code includes intentionally extensive teaching comments.

## Instructor prompt

`My VPN is failing and I want to know the status of INC-1042.`

**Important:** This is an educational demonstration, not a complete enterprise help-desk application. No real ticket or VPN system is connected.

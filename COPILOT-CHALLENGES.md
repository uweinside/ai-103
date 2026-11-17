# Copilot follow-on tasks (trainer challenges)

Use the following prompts in GitHub Copilot Agent mode. Require explanations of API selections and full build verification.

## 01 — Direct API

> Extend `01-DirectResponses` to show both Chat Completions and Responses calls for the exact same prompt. Explain what each API stores, whether history is sent automatically, and how to count input/output tokens. Keep the original code as the teaching baseline.

## 02 — Tool calling

> Add a second **read-only** function `get_vpn_policy` that retrieves from a local fake `vpn-policy.md`. Explain schema choice and validate tool arguments. Add a hard cap on model tool-calling rounds, log the call IDs, and never execute arbitrary tool names.

> Next, introduce MCP via the official C# MCP client SDK using a server with a test-only ticket lookup. Explain how discovery differs from direct function registration; preserve manual control over permissions.

## 03 — Managed agent

> Add a Foundry IQ knowledge connection to the persisted declarative agent using the currently supported SDK pattern. Put the required Foundry provisioning steps in README and verify the model/tool support rather than inventing a connection ID.

## 04 — Workflow and operations

> Add a third agent that checks for policy violations. Build a branch with a human approval gate before any action that would change a ticket. Add OpenTelemetry spans for each agent transition. Keep the initial demo read-only.

## 05 — MCP client and agent

> Compare app 05's discovered MCP schema and remote dispatch with app 02's local function schema and manual loop. Add an evaluation that checks observed tool use and returned source links without snapshotting model prose. Preserve the read-only search allowlist and do not send private context to Learn.

## Testing and teaching

> Produce a short live-demo script for each app, show what output to expect, list two intentionally broken variants, and add a smoke-test command. Avoid snapshotting stochastic LLM text verbatim.

# Sample 01B - Responses SDK

The same model conversation as [sample 01](../01-DirectResponses/README.md),
using the typed **OpenAI .NET SDK** instead of raw `HttpClient` and JSON.
This is not Agent Framework or a persisted Foundry agent.
The pinned SDK marks its Responses types experimental; this sample explicitly
opts in, following Microsoft Learn. See the compatibility notes in `SOURCES.md`.

## Run

From repository root, using the same Azure configuration as sample 01:

```powershell
.\scripts\run.ps1 1b
```

Or, after loading the environment variables described in [SETUP.md](../SETUP.md):

```powershell
dotnet run --project .\01B-ResponsesSdk
```

The Bash launcher also accepts `1b`. No new variables or API keys are needed.
Only fictional prompts should be sent to the configured Azure model; no ticket
system, company policy source, tools, or production writes are connected.
Azure model usage can incur charges, just as in sample 01.

## Compare side by side

| Sample 01: REST | Sample 01B: SDK | Ownership remains |
|---|---|---|
| `HttpClient`, bearer header | `ResponsesClient`, `BearerTokenPolicy` | Application selects identity and endpoint; Entra/RBAC controls access |
| Dictionary serialized to JSON | `CreateResponseOptions` and typed input items | Application chooses deployment, instructions and user turn |
| `previous_response_id`, `store` | `PreviousResponseId`, `StoredOutputEnabled` | Application links turns; Responses service stores state |
| `JsonDocument` output traversal | `ResponseResult.GetOutputText()` | SDK extracts text; application interprets output |
| HTTP status check | `ClientResultException` | Application reports failure and retains the previous successful chain |

Set a breakpoint on `CreateResponseAsync`. Inspect `options`, then
`response.OutputItems` and `response.Id`. Instructions are resent on every call;
the SDK does not automatically remember them or maintain our conversation loop.
The endpoint setting is still a **resource root**; this app adds `/openai/v1/`
and the SDK adds `responses`.

## Instructor walkthrough

1. Ask `What is our VPN policy, and what is the status of ticket INC-1042?`
   The assistant should acknowledge that it cannot access either source.
2. Ask `What can I safely check myself?` Compare the previous/stored ID traces:
   only the new message is sent by this application; Azure resolves the chain.
3. Type `/new`, then ask `What ticket number did I mention?` The new request
   has no previous ID. Resetting the local pointer does not delete stored
   responses, change model training, or guarantee a particular model answer.
4. Blank lines make no request; `/quit` or end-of-input exits. HTTP errors are
   reported without advancing the chain. Credential/transport failures propagate.
   A non-completed model response is reported separately from an HTTP failure.

The higher-level client removes transport boilerplate, not the need to design
state retention, costs, error handling, access boundaries, or output validation.
Continue to app 02 for explicit application-owned function-call dispatch.

See [SOURCES.md](../SOURCES.md) for the pinned SDK version, current Microsoft
Learn examples, and compatibility notes.

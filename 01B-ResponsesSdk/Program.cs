// The pinned SDK marks Responses APIs experimental even in a stable package.
// Opt in only in this sample, as in Microsoft Learn; review SOURCES.md on upgrade.
#pragma warning disable OPENAI001

using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.Identity;
using OpenAI.Responses;

// -----------------------------------------------------------------------------
// LESSON 1B: SAME RESPONSES SERVICE, HIGHER-LEVEL CLIENT
// -----------------------------------------------------------------------------
// Compare with 01-DirectResponses: the SDK replaces manual HTTP headers, JSON
// serialization and output traversal, NOT the application's conversation loop.
// This is still a model request, not a persisted Foundry agent or an AIAgent.
// The application selects instructions and links turns; the Responses service
// stores response state. Neither that state nor an SDK makes the LLM learn our
// conversation or gives it access to internal company policies and tickets.
// -----------------------------------------------------------------------------

string endpoint = Required("AZURE_OPENAI_ENDPOINT").TrimEnd('/');
string deployment = Required("AZURE_OPENAI_DEPLOYMENT_NAME");

// Keep App 01's resource-root configuration. The SDK needs the /openai/v1/
// BASE URL and appends "responses" itself; supplying the full REST operation URL
// here would duplicate the route. No project-management client is involved.
// Reusing the credential and bearer policy lets the SDK obtain/refresh Entra
// tokens. Do not wrap a one-time token in ApiKeyCredential: it would expire.
// The scope matches App 01's Azure OpenAI audience; RBAC is still required.
// .env.example selects AzureCliCredential in this chain for local az login.
DefaultAzureCredential credential = new();
ResponsesClient client = new(
    authenticationPolicy: new BearerTokenPolicy(
        credential, "https://cognitiveservices.azure.com/.default"),
    options: new ResponsesClientOptions
    {
        Endpoint = new Uri(endpoint + "/openai/v1/"),
        NetworkTimeout = TimeSpan.FromSeconds(120)
    });

Console.WriteLine("01B - RESPONSES SDK");
Console.WriteLine("Prompt idea: Explain our VPN policy. Then ask a follow-up.");
Console.WriteLine("Type /new to start a new response chain, or /quit to exit.\n");

string? previousId = null;
while (true)
{
    Console.Write("You > ");
    string? input = Console.ReadLine();
    if (input is null || input.Trim().Equals("/quit", StringComparison.OrdinalIgnoreCase)) break;
    if (input.Trim().Equals("/new", StringComparison.OrdinalIgnoreCase))
    {
        previousId = null;
        Console.WriteLine("Started new chain. This did NOT delete stored responses or erase model training.\n");
        continue;
    }
    if (string.IsNullOrWhiteSpace(input)) continue;

    // Typed options map to the SAME REST fields seen in App 01. Instructions
    // are sent on every turn: previous_response_id does not carry them forward.
    // Only the new message is sent locally; the service resolves the prior ID.
    // Stored state has retention and token costs; /new only clears our pointer.
    CreateResponseOptions options = new()
    {
        Model = deployment,
        Instructions = "You are a cautious IT support assistant. You cannot access the company's internal policies or ticketing system. State that limitation when relevant. Respond concisely.",
        InputItems = { ResponseItem.CreateUserMessageItem(input) },
        StoredOutputEnabled = true,
        PreviousResponseId = previousId
    };
    Console.WriteLine($"[Teaching trace] Previous response ID: {previousId ?? "(none)"}");

    try
    {
        ResponseResult response = await client.CreateResponseAsync(options);

        // HTTP success is not necessarily a completed model response. Do not
        // silently present failed/incomplete output as an answer or advance our
        // chain. Keep the last successful ID so the instructor can retry/reset.
        if (response.Status != ResponseStatus.Completed)
        {
            Console.Error.WriteLine($"Response {response.Id} did not complete (status: {response.Status}).");
            Console.Error.WriteLine("Inspect the response in the debugger; the previous chain is unchanged.\n");
            continue;
        }

        previousId = response.Id;

        // GetOutputText aggregates text across output items; output[0] need not
        // be text. The helper does NOT execute tool calls or validate the truth
        // of the generated answer. No tools or knowledge sources are connected.
        string text = response.GetOutputText();
        if (string.IsNullOrWhiteSpace(text))
            Console.Error.WriteLine("No output text returned; inspect response.OutputItems in the debugger.");
        else
            Console.WriteLine("Assistant > " + text);
        Console.WriteLine($"[Teaching trace] Stored response ID: {previousId}\n");
    }
    catch (ClientResultException ex) when (ex.Status > 0)
    {
        // Unlike raw HttpClient's status check, SDK HTTP failures throw. Report
        // the service error and keep the last successful chain, as in App 01.
        // Credential/transport failures propagate rather than triggering an
        // anonymous/API-key fallback. Never print tokens or request headers.
        Console.Error.WriteLine($"HTTP {ex.Status}: {ex.Message}");
        Console.Error.WriteLine("Check route, deployment, model/region support and RBAC. Use /new if a stored response expired.\n");
    }
}

static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
    ? value : throw new InvalidOperationException($"Missing {name}; see SETUP.md at workspace root.");

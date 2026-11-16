using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;

// -----------------------------------------------------------------------------
// LESSON 1: THE MODEL IS NOT THE APPLICATION
// -----------------------------------------------------------------------------
// Here we deliberately use HttpClient instead of a higher-level OpenAI SDK.
// This reveals the actual REST contract: endpoint, model deployment, messages,
// returned response ID and the fact that *we* compose every new user turn.
//
// The Responses service may retain previous responses that are referenced via
// previous_response_id. That is NOT the same as the underlying LLM learning or
// permanently remembering a conversation. State scope, retention, token costs
// and availability must be designed explicitly in real systems.
// -----------------------------------------------------------------------------

string endpoint = Required("AZURE_OPENAI_ENDPOINT").TrimEnd('/');
string deployment = Required("AZURE_OPENAI_DEPLOYMENT_NAME");
string url = endpoint + "/openai/v1/responses";

using HttpClient http = new() { Timeout = TimeSpan.FromSeconds(120) };
http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

// Authentication belongs to the application, not the model. DefaultAzureCredential
// can use the developer identity established by az login (or another supported
// identity in its chain). The example .env selects AzureCliCredential in that
// chain so another developer sign-in cannot take precedence. Azure RBAC separately
// decides what that identity may do.
// Reuse the credential so Azure.Identity can cache tokens; never use an API key.
DefaultAzureCredential credential = new();
TokenRequestContext tokenContext = new(["https://cognitiveservices.azure.com/.default"]);

Console.WriteLine("01 — DIRECT RESPONSES API");
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
        Console.WriteLine("Started new chain. This did NOT erase anything in model training.\n");
        continue;
    }
    if (string.IsNullOrWhiteSpace(input)) continue;

    // Notice that instructions are supplied on EVERY request. In the Responses
    // API, do not assume instructions from a prior call automatically persist.
    // The model should not invent privileged internal policy information.
    var request = new Dictionary<string, object?>
    {
        ["model"] = deployment,
        ["instructions"] = "You are a cautious IT support assistant. You cannot access the company's internal policies or ticketing system. State that limitation when relevant. Respond concisely.",
        ["input"] = input,
        ["store"] = true  // Enable stored response linkage for the demonstration.
    };
    if (previousId is not null) request["previous_response_id"] = previousId;

    // Raw HttpClient has no Azure SDK authentication policy. Ask the credential
    // for a token before EVERY request so it can refresh an expiring cached token.
    // If authentication fails, the exception stops execution before sending the
    // prompt; there is no anonymous or API-key fallback, and tokens are not logged.
    AccessToken token = await credential.GetTokenAsync(tokenContext);
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
    using HttpResponseMessage response = await http.PostAsJsonAsync(url, request);
    string raw = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"HTTP {(int)response.StatusCode}: {raw}");
        Console.Error.WriteLine("Check route, deployment, model/region support and RBAC.\n");
        continue;
    }

    using JsonDocument doc = JsonDocument.Parse(raw);
    JsonElement root = doc.RootElement;
    previousId = root.TryGetProperty("id", out var id) ? id.GetString() : null;

    // A Responses API output is an ARRAY of items. An answer can contain text,
    // reasoning, tool calls or other item types. Never assume output[0] is text.
    Console.WriteLine("Assistant > " + ExtractText(root));
    Console.WriteLine($"[Teaching trace] Stored response ID: {previousId ?? "(none)"}\n");
}

static string ExtractText(JsonElement root)
{
    List<string> parts = [];
    if (!root.TryGetProperty("output", out var items) || items.ValueKind != JsonValueKind.Array)
        return "(No output array; inspect the raw JSON.)";
    foreach (JsonElement item in items.EnumerateArray())
    {
        if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array) continue;
        foreach (JsonElement part in content.EnumerateArray())
        {
            if (part.TryGetProperty("type", out var type) && type.GetString() == "output_text" &&
                part.TryGetProperty("text", out var text)) parts.Add(text.GetString() ?? "");
        }
    }
    return parts.Count == 0 ? "(No output_text; model may have returned other items.)" : string.Join("\n", parts);
}
static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
    ? value : throw new InvalidOperationException($"Missing {name}; see SETUP.md at workspace root.");

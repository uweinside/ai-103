using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;

// -----------------------------------------------------------------------------
// LESSON 2: FUNCTION CALLING IS A PROTOCOL, NOT REMOTE CODE EXECUTION
// -----------------------------------------------------------------------------
// The LLM returns a structured *request* to call a tool. It cannot execute this
// C# function on its own. YOUR application must authorize the tool, validate
// arguments, call the function and send the output back as another model input.
//
// The local ticket database below is intentionally fictional and read-only.
// This is where to discuss least privilege, prompt injection, approvals and
// why an arbitrary function name from a model must NEVER be executed as code.
// -----------------------------------------------------------------------------

string endpoint = Required("AZURE_OPENAI_ENDPOINT").TrimEnd('/');
string deployment = Required("AZURE_OPENAI_DEPLOYMENT_NAME");
using HttpClient http = new() { Timeout = TimeSpan.FromSeconds(120) };
http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
// The application authenticates, not the model or the local ticket function.
// DefaultAzureCredential supports the developer identity from az login as part
// of its credential chain. The example .env selects AzureCliCredential so a
// different developer sign-in cannot take precedence. RBAC is a separate check.
// Reuse this instance for token caching; no API key is accepted by this sample.
DefaultAzureCredential credential = new();
string url = endpoint + "/openai/v1/responses";

// JSON Schema is the public interface the MODEL sees. It is not the execution
// implementation. Tool descriptions should be precise and non-overlapping.
var ticketTool = new
{
    type = "function",
    name = "get_ticket_status",
    description = "Read the status of a fictional IT help-desk ticket by ticket ID. Does not modify tickets.",
    strict = true,
    parameters = new
    {
        type = "object",
        properties = new
        {
            ticket_id = new { type = "string", description = "IT ticket identifier, e.g. INC-1042" }
        },
        required = new[] { "ticket_id" },
        additionalProperties = false
    }
};

Console.WriteLine("02 — MANUAL TOOL DISPATCH");
Console.WriteLine("Example: What is the status of ticket INC-1042?");
Console.WriteLine("Enter /quit to exit. This demo handles each question as a fresh chain.\n");
while (true)
{
    Console.Write("You > ");
    string? question = Console.ReadLine();
    if (question is null || question.Equals("/quit", StringComparison.OrdinalIgnoreCase)) break;
    if (string.IsNullOrWhiteSpace(question)) continue;

    // This request tells the MODEL about a tool. It does not register C# code
    // with Azure or grant access to any back-end system. Strict schemas should
    // not be combined with parallel tool calls: Microsoft recommends disabling
    // them so the returned arguments can follow the schema. This intentionally
    // limits this response to at most one tool request, not tool execution.
    var first = new
    {
        model = deployment,
        instructions = "You are an IT support assistant. Use get_ticket_status for ticket status questions. Do not make up ticket states. Never claim to have changed a ticket.",
        input = question,
        tools = new[] { ticketTool },
        tool_choice = "auto",
        parallel_tool_calls = false,
        store = true
    };

    using JsonDocument firstReply = await SendAsync(http, credential, url, first);
    JsonElement firstRoot = firstReply.RootElement;
    string? responseId = firstRoot.GetProperty("id").GetString();

    // This request allows zero or one tool call. Still inspect every output item:
    // the response may contain reasoning or text, and other configurations can
    // permit multiple calls. Only application code can authorize and execute one.
    List<(string CallId, string Name, string Arguments)> calls = [];
    foreach (JsonElement item in firstRoot.GetProperty("output").EnumerateArray())
    {
        if (item.TryGetProperty("type", out var t) && t.GetString() == "function_call")
        {
            calls.Add((item.GetProperty("call_id").GetString()!,
                       item.GetProperty("name").GetString()!,
                       item.GetProperty("arguments").GetString() ?? "{}"));
        }
    }

    if (calls.Count == 0)
    {
        Console.WriteLine("Assistant > " + ExtractText(firstRoot) + "\n");
        continue;
    }

    // Pause this at a breakpoint! The next loop is the TRUST BOUNDARY.
    List<object> toolResults = [];
    foreach (var call in calls)
    {
        Console.WriteLine($"[Model REQUESTED] {call.Name}({call.Arguments}), call_id={call.CallId}");
        string result;
        if (call.Name != "get_ticket_status")
        {
            // An allowlist is safer than dynamically locating a method by name.
            result = JsonSerializer.Serialize(new { error = "Unknown or unauthorized tool" });
        }
        else
        {
            try
            {
                using JsonDocument parsed = JsonDocument.Parse(call.Arguments);
                string id = parsed.RootElement.GetProperty("ticket_id").GetString() ?? "";
                if (!System.Text.RegularExpressions.Regex.IsMatch(id, @"^INC-[0-9]{4}$"))
                    throw new ArgumentException("Invalid ticket ID pattern");
                result = LookupTicket(id);  // This is the actual C# method execution.
            }
            catch (Exception ex) when (ex is JsonException or ArgumentException or KeyNotFoundException)
            {
                result = JsonSerializer.Serialize(new { error = "Invalid tool arguments" });
            }
        }
        Console.WriteLine("[C# DISPATCH RESULT] " + result);
        toolResults.Add(new { type = "function_call_output", call_id = call.CallId, output = result });
    }

    // A tool result is NOT the final answer. Send it back into the response
    // chain so the MODEL can formulate the user-facing natural-language reply.
    using JsonDocument finalReply = await SendAsync(http, credential, url, new
    {
        model = deployment,
        previous_response_id = responseId,
        input = toolResults,
        store = true
    });
    Console.WriteLine("Assistant > " + ExtractText(finalReply.RootElement) + "\n");

    // Production designs need to handle a model requesting another tool after
    // this response. This sample stops after one round on purpose. See the
    // Copilot challenge for a bounded multi-round loop with full logging.
}

static string LookupTicket(string id)
{
    // Keep sample data deterministic so the student can check the LLM's answer.
    var tickets = new Dictionary<string, (string Status, string Owner, string Note)>
    {
        ["INC-1042"] = ("In progress", "Network operations", "VPN login failure; diagnostics underway"),
        ["INC-1207"] = ("Resolved", "Service desk", "Password reset completed"),
        ["INC-1888"] = ("Waiting for user", "Endpoint support", "Please confirm device OS")
    };
    return tickets.TryGetValue(id, out var t)
        ? JsonSerializer.Serialize(new { ticket_id = id, status = t.Status, owner = t.Owner, note = t.Note })
        : JsonSerializer.Serialize(new { ticket_id = id, error = "Not found" });
}
static async Task<JsonDocument> SendAsync(HttpClient http, TokenCredential credential, string url, object payload)
{
    // HttpClient does not refresh Entra tokens automatically. Obtain a token for
    // each request, including the tool-result turn; Azure.Identity caches and
    // refreshes it. Authentication errors propagate before sending the payload,
    // with no API-key fallback. Never print the token or Authorization header.
    AccessToken token = await credential.GetTokenAsync(
        new TokenRequestContext(["https://cognitiveservices.azure.com/.default"]), CancellationToken.None);
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
    using HttpResponseMessage response = await http.PostAsJsonAsync(url, payload);
    string body = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode) throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {body}");
    return JsonDocument.Parse(body);
}
static string ExtractText(JsonElement root)
{
    List<string> texts = [];
    if (!root.TryGetProperty("output", out var output)) return "(No output)";
    foreach (var item in output.EnumerateArray())
    {
        if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array) continue;
        foreach (var chunk in content.EnumerateArray())
            if (chunk.TryGetProperty("type", out var type) && type.GetString() == "output_text" &&
                chunk.TryGetProperty("text", out var text)) texts.Add(text.GetString() ?? "");
    }
    return texts.Count == 0 ? "(No output text; inspect output items)" : string.Join("\n", texts);
}
static string Required(string n) => Environment.GetEnvironmentVariable(n) is { Length: > 0 } value
    ? value : throw new InvalidOperationException($"Missing {n}; see SETUP.md.");

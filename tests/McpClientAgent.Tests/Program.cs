using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

// An in-memory HTTP handler exercises our dispatch/error policy without opening
// a socket or depending on Azure, the public Learn server or stochastic prose.
using var handler = new McpFixtureHandler();
using var httpClient = new HttpClient(handler);
await using McpClient client = await McpClient.CreateAsync(new HttpClientTransport(
    new HttpClientTransportOptions
    {
        Endpoint = new Uri("https://mcp-test.invalid/mcp"),
        TransportMode = HttpTransportMode.StreamableHttp
    }, httpClient));
McpClientTool discovered = (await client.ListToolsAsync()).Single();
var arguments = new AIFunctionArguments { ["query"] = "public teaching query" };

var budgeted = new ObservedMcpTool(discovered, client);
Check(budgeted.JsonSchema.GetRawText() == discovered.JsonSchema.GetRawText(),
    "Wrapper must retain the discovered schema.");
for (int i = 0; i < 3; i++)
{
    object? result = await budgeted.InvokeAsync(arguments);
    Check(result is CallToolResult { IsError: not true }, "Successful tool response must reach the caller.");
}
await ThrowsAsync<InvalidOperationException>(() => budgeted.InvokeAsync(arguments).AsTask());
Check(handler.ToolCalls == 3, "The fourth attempt must not reach the server.");
Check(budgeted.SuccessfulCalls == 3 && budgeted.FailedCalls == 1, "Budget rejection must fail the run.");

handler.Mode = "tool-error";
var toolError = new ObservedMcpTool(discovered, client);
await ThrowsAsync<InvalidOperationException>(() => toolError.InvokeAsync(arguments).AsTask());
Check(toolError.SuccessfulCalls == 0 && toolError.FailedCalls == 1,
    "HTTP success with isError=true must not count as successful grounding.");

handler.Mode = "success";
var partial = new ObservedMcpTool(discovered, client);
await partial.InvokeAsync(arguments);
handler.Mode = "protocol-error";
await ThrowsAsync<McpException>(() => partial.InvokeAsync(arguments).AsTask());
Check(partial.SuccessfulCalls == 1 && partial.FailedCalls == 1,
    "An SDK exception after a successful call must still prevent a successful run.");
Console.WriteLine("Passed: discovered schema/dispatch, call budget, tool error, and partial-run protocol error.");

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception
{
    try
    {
        await action();
    }
    catch (T)
    {
        return;
    }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

file sealed class McpFixtureHandler : HttpMessageHandler
{
    public string Mode { get; set; } = "success";
    public int ToolCalls { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Post)
            return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);

        JsonNode body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
        string method = body["method"]!.GetValue<string>();
        if (method == "notifications/initialized")
            return new HttpResponseMessage(HttpStatusCode.Accepted);

        JsonNode result;
        if (method == "server/discover")
        {
            // Advertise no newer discovery method so the SDK negotiates the
            // initialize handshake supported by this deliberately small fixture.
            result = new JsonObject();
        }
        else if (method == "initialize")
        {
            result = new JsonObject
            {
                ["protocolVersion"] = body["params"]!["protocolVersion"]!.DeepClone(),
                ["capabilities"] = new JsonObject { ["tools"] = new JsonObject() },
                ["serverInfo"] = new JsonObject { ["name"] = "fictional-test-server", ["version"] = "1" }
            };
        }
        else if (method == "tools/list")
        {
            result = JsonNode.Parse("""
                {"tools":[{"name":"microsoft_docs_search","description":"Fictional search fixture.",
                "inputSchema":{"type":"object","properties":{"query":{"type":"string"}},"required":["query"]}}]}
                """)!;
        }
        else if (method == "tools/call")
        {
            ToolCalls++;
            if (body["params"]!["name"]!.GetValue<string>() != "microsoft_docs_search" ||
                body["params"]!["arguments"]!["query"]!.GetValue<string>() != "public teaching query")
                throw new InvalidOperationException("Dispatch changed the selected tool or its arguments.");
            result = JsonNode.Parse(Mode == "tool-error"
                ? """{"isError":true,"content":[{"type":"text","text":"Fictional tool failure."}]}"""
                : """{"content":[{"type":"text","text":"Fictional documentation result."}]}""")!;
        }
        else
        {
            throw new InvalidOperationException($"Unexpected fixture request: {method}");
        }

        var response = new JsonObject { ["jsonrpc"] = "2.0", ["id"] = body["id"]!.DeepClone() };
        if (method == "server/discover")
            response["error"] = new JsonObject { ["code"] = -32601, ["message"] = "Method not found." };
        else if (method == "tools/call" && Mode == "protocol-error")
            response["error"] = new JsonObject { ["code"] = -32603, ["message"] = "Fictional protocol failure." };
        else
            response["result"] = result;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(response.ToJsonString(), Encoding.UTF8, "application/json")
        };
    }
}

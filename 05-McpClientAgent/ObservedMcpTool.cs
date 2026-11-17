using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

// Delegation preserves the discovered tool's name, description and JSON schema.
// This wrapper is the visible application-owned dispatch boundary, not a local
// reimplementation of Learn search. Only the allowlisted instance is registered.
internal sealed class ObservedMcpTool(McpClientTool inner, McpClient client) : DelegatingAIFunction(inner)
{
    private int attempts;
    private int successfulCalls;
    public int SuccessfulCalls => Volatile.Read(ref successfulCalls);
    // Read after the agent run completes: any attempt without a successful
    // response counts as failed, including transport/timeout exceptions that
    // the framework might have caught and returned to the model.
    public int FailedCalls => Volatile.Read(ref attempts) - SuccessfulCalls;

    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        // Bound remote work even if the model repeatedly requests calls. Count
        // attempts atomically because a model can request parallel functions.
        // The framework may relay function exceptions to the model for recovery;
        // stderr plus FailedCalls prevent that becoming a hidden successful run.
        if (Interlocked.Increment(ref attempts) > 3)
        {
            Console.Error.WriteLine("[runtime] MCP call budget exceeded.");
            throw new InvalidOperationException("This lesson permits at most three MCP calls.");
        }

        Console.WriteLine($"[runtime -> MCP client] tools/call: {Name}");
        // No argument logging: even read-only search transmits arguments to the
        // remote operator. These fixed public prompts are not a general-purpose
        // data-loss prevention boundary for arbitrary future user input.
        CallToolResult result = await client.CallToolAsync(
            Name, arguments, cancellationToken: cancellationToken);
        if (result.IsError == true)
        {
            Console.Error.WriteLine($"[MCP server] {Name} returned isError=true; not grounding evidence.");
            throw new InvalidOperationException($"MCP tool {Name} reported an execution error.");
        }

        Interlocked.Increment(ref successfulCalls);
        Console.WriteLine($"[MCP server -> runtime] {Name} succeeded; return reference data to the caller.");
        return result;
    }
}

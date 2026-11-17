using System.Text.Json;
using OpenAI.Responses;

#pragma warning disable OPENAI001
internal static class GroundingEvidence
{
    // Transport success, tool success and answer quality are different checks.
    // These guards reject an absent/failed call even if the model apologizes or
    // produces fluent prose. Empty search results can legitimately succeed:
    // they should lead to abstention, not invented hotels. The instructor still
    // checks the actual retrieved facts and citations against the original PDF.
    public static McpToolCallItem[] Validate(ResponseResult response)
    {
        if (response.Status != ResponseStatus.Completed || response.Error is not null)
            throw new InvalidOperationException("The agent response did not complete successfully.");
        McpToolCallItem[] calls = response.OutputItems.OfType<McpToolCallItem>().ToArray();
        if (calls.Length == 0)
            throw new InvalidOperationException("No MCP retrieval was observed; refusing an ungrounded answer.");
        foreach (McpToolCallItem call in calls)
        {
            if (call.ServerLabel != "travel-brochures" || call.ToolName != "knowledge_base_retrieve"
                || call.Error is not null || string.IsNullOrWhiteSpace(call.ToolOutput))
                throw new InvalidOperationException("Foundry IQ retrieval failed or an unexpected tool was called.");
            // Some MCP execution failures arrive inside a successful HTTP
            // response. Do not treat isError=true as usable grounding evidence.
            using JsonDocument result = JsonDocument.Parse(call.ToolOutput);
            if (result.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("Foundry IQ returned an unexpected tool result shape.");
            if (result.RootElement.TryGetProperty("isError", out JsonElement isError)
                && isError.ValueKind == JsonValueKind.True)
                throw new InvalidOperationException("Foundry IQ returned an MCP tool execution error.");
            if (!result.RootElement.TryGetProperty("documents", out JsonElement documents)
                || documents.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Foundry IQ did not return its expected documents array.");
        }
        return calls;
    }
}

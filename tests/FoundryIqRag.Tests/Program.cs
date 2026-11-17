using OpenAI.Responses;

#pragma warning disable OPENAI001
// These tests assert the application's evidence gate, not stochastic model
// prose, Azure behavior or factual citation correctness. No network is used.
var missing = new ResponseResult { Status = ResponseStatus.Completed };
Rejects(missing, "A model-only response must not be presented as grounded.");

ResponseResult success = WithCall("""{"documents":[{"id":"test-source","content":"Thameside House: river views and breakfast."}]}""");
if (GroundingEvidence.Validate(success).Length != 1)
    throw new InvalidOperationException("Successful retrieval must expose its evidence.");

ResponseResult empty = WithCall("""{"documents":[]}""");
GroundingEvidence.Validate(empty); // No matches is valid evidence for abstention.
Rejects(WithCall("""{"isError":true,"documents":[]}"""), "MCP execution errors must fail even with HTTP success.");
Rejects(WithCall("""{"error":"Retrieval unavailable"}"""), "Unexpected output must not count as evidence.");
ResponseResult failed = WithCall("""{"documents":[]}""");
((McpToolCallItem)failed.OutputItems[0]).Error = BinaryData.FromString("""{"message":"Forbidden"}""");
Rejects(failed, "A server-side MCP error must not be ignored.");
ResponseResult unexpected = WithCall("""{"documents":[]}""");
((McpToolCallItem)unexpected.OutputItems[0]).ToolName = "book_hotel";
Rejects(unexpected, "Only the configured read-only retrieval tool is permitted.");
ResponseResult incomplete = WithCall("""{"documents":[]}""");
incomplete.Status = ResponseStatus.Incomplete;
Rejects(incomplete, "Partial model output is not a completed grounded run.");
ResponseResult partialFailure = WithCall("""{"documents":[]}""");
partialFailure.OutputItems.Add(new McpToolCallItem("travel-brochures", "knowledge_base_retrieve", BinaryData.FromString("{}"))
{
    ToolOutput = """{"isError":true}"""
});
Rejects(partialFailure, "A successful call must not mask a later failed call.");
Console.WriteLine("Passed: evidence gate, missing retrieval, empty results, tool errors, allowlist, and incomplete/partial failure.");

static ResponseResult WithCall(string output)
{
    var result = new ResponseResult { Status = ResponseStatus.Completed };
    result.OutputItems.Add(new McpToolCallItem("travel-brochures", "knowledge_base_retrieve", BinaryData.FromString("{}"))
    {
        ToolOutput = output
    });
    return result;
}

static void Rejects(ResponseResult response, string message)
{
    try { GroundingEvidence.Validate(response); }
    catch (InvalidOperationException) { return; }
    throw new InvalidOperationException(message);
}

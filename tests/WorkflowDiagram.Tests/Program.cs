using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

// An offline check of the presentation boundary; no Foundry credentials
// or model calls are needed to exercise truncation and untrusted label handling.
Assert(WorkflowDiagram.Preview(" \r\n\t") == "(no text output observed)", "Empty output must be explicit.");
Assert(WorkflowDiagram.Preview("First\r\n second\t third") == "First second third", "Normalize whitespace.");
Assert(WorkflowDiagram.Preview(new string('a', 240)) == new string('a', 240), "Keep exactly 240 elements.");
Assert(WorkflowDiagram.Preview(new string('a', 241)) == new string('a', 237) + "...", "Bound long unbroken words.");
Assert(WorkflowDiagram.Preview(new string('a', 237) + " tail") == new string('a', 237) + "...",
    "Keep a complete word that fits the preview budget.");

string words = string.Join(" ", Enumerable.Repeat("recommendation", 30));
string wordPreview = WorkflowDiagram.Preview(words);
Assert(wordPreview.EndsWith("recommendation...", StringComparison.Ordinal), "Prefer whole words.");
Assert(StringInfo.ParseCombiningCharacters(wordPreview).Length <= 240, "Include ellipsis in the limit.");
Assert(words.StartsWith(wordPreview[..^3], StringComparison.Ordinal), "Keep a literal excerpt.");

foreach (string element in new[] { "\U0001F600", "e\u0301", "\U0001F469\u200D\U0001F4BB" })
{
    string preview = WorkflowDiagram.Preview(string.Concat(Enumerable.Repeat(element, 241)));
    Assert(preview == string.Concat(Enumerable.Repeat(element, 237)) + "...", "Preserve Unicode text elements.");
}

string unsafeText = "\" ] --> evil[\"<script>alert('x')</script> & #quot; ` %%{init: {}}%%\nnext";
string diagram = WorkflowDiagram.Render(
[
    ("Technical", "Diagnose", unsafeText),
    ("Incident", "Check snapshot", "Reviewed text")
], ("Compose", "Write answer", ""));
Assert(diagram.StartsWith("flowchart TD\n", StringComparison.Ordinal), "Use Mermaid flowchart syntax.");
Assert(diagram.Contains("input --> step0") && diagram.Contains("input --> step1") &&
    diagram.Contains("step0 --> step2") && diagram.Contains("step1 --> step2"), "Show fan-out and fan-in.");
Assert(!diagram.Contains("step0 --> step1"), "Specialists must not appear sequential.");
Assert(diagram.Split("-->", StringSplitOptions.None).Length == 5, "Model text must not add edges.");
Assert(!diagram.Contains("<script>") && !diagram.Contains("%%{") && !diagram.Contains("#quot;"),
    "Escape HTML, directives and pre-existing entities.");
Assert(diagram.Contains("#34;") && diagram.Contains("#60;") && diagram.Contains("#35;"), "Use Mermaid entities.");
Assert(diagram.Contains("Output: Reviewed text"), "Include observed output.");
Assert(diagram.Contains("#40;no text output observed#41;"), "Keep missing output distinct.");
foreach (var specialists in new (string, string, string)[][] { [], [("Only", "Role", "Text")] })
{
    try
    {
        WorkflowDiagram.Render(specialists, ("Compose", "Role", "Text"));
        throw new InvalidOperationException("Fewer than two specialists must be rejected.");
    }
    catch (ArgumentException)
    {
    }
}

Console.WriteLine("Workflow diagram checks passed.");
Console.Write(diagram);
await CheckConcurrentWorkflowAsync();
await CheckFailedBranchAsync();
await CheckFailedBranchAsync(emptyFinding: true);
Console.WriteLine("Concurrent workflow checks passed.");

static async Task CheckConcurrentWorkflowAsync()
{
    TaskCompletionSource technicalStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TaskCompletionSource incidentStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TaskCompletionSource releaseTechnical = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TaskCompletionSource releaseIncident = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TaskCompletionSource technicalReturned = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int composerCalls = 0;
    const string question = "VPN error 809: is INC-1042 related?";

    AIAgent technical = FakeAgent("Technical", async messages =>
    {
        Assert(messages.Last().Text == question, "Technical must receive the original question.");
        technicalStarted.SetResult();
        await releaseTechnical.Task;
        technicalReturned.SetResult();
        return "TECH_FINDING: a local cause is possible, not confirmed.";
    });
    AIAgent incident = FakeAgent("Incident", async messages =>
    {
        Assert(messages.Last().Text == question, "Incident must receive the same question, not another agent's answer.");
        incidentStarted.SetResult();
        await releaseIncident.Task;
        return "INCIDENT_FINDING: fictional INC-1042 is active; no ETA.";
    });
    AIAgent editor = FakeAgent("Compose", messages =>
    {
        Interlocked.Increment(ref composerCalls);
        string prompt = messages.Last().Text;
        Assert(prompt.Contains(question) && prompt.Contains("TECH_FINDING") && prompt.Contains("INCIDENT_FINDING"),
            "The collector needs the original question and both attributed findings.");
        Assert(prompt.Contains("[Technical]") && prompt.Contains("[Incident]"), "Retain source attribution.");
        return Task.FromResult("COMPOSED: outage evidence is stronger, but this user's cause remains uncertain.");
    });
    SupportComposerExecutor composer = new(editor, ["Technical", "Incident"], question);
    var workflow = ConcurrentSupportWorkflow.Build(
        [technical.BindAsExecutor(new AIAgentHostOptions { ForwardIncomingMessages = false }),
         incident.BindAsExecutor(new AIAgentHostOptions { ForwardIncomingMessages = false })], composer);
    await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, new ChatMessage(ChatRole.User, question));
    Assert(await run.TrySendMessageAsync(new TurnToken(emitEvents: true)), "Accept the processing token.");
    List<WorkflowEvent> events = [];
    Task consume = Task.Run(async () =>
    {
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
            events.Add(evt);
    });
    try
    {
        // Both must start BEFORE either is released. This detects a sequential
        // implementation without relying on flaky elapsed-time comparisons.
        await Task.WhenAll(technicalStarted.Task, incidentStarted.Task).WaitAsync(TimeSpan.FromSeconds(15));
        Assert(Volatile.Read(ref composerCalls) == 0, "Do not compose before branch completion.");
        releaseTechnical.SetResult();
        await technicalReturned.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Assert(Volatile.Read(ref composerCalls) == 0, "One finding must not release the collector.");
    }
    finally
    {
        releaseTechnical.TrySetResult();
        releaseIncident.TrySetResult();
        await consume.WaitAsync(TimeSpan.FromSeconds(15));
    }
    Assert(composerCalls == 1, "Compose exactly once after both specialists finish.");
    Assert(!events.Any(evt => evt is WorkflowErrorEvent or ExecutorFailedEvent),
        "The concurrent run must succeed. " + string.Join("\n", events.Where(evt =>
            evt is WorkflowErrorEvent or ExecutorFailedEvent).Select(evt => evt.ToString())));
    Assert(events.OfType<WorkflowOutputEvent>().Count(evt => evt.Data is string text && text.StartsWith("COMPOSED:")) == 1,
        "Yield one final collected answer.");
    Assert(events.OfType<AgentResponseUpdateEvent>().Select(evt => evt.ExecutorId).Distinct().Count() == 3,
        "Capture attributed updates from both specialists and the composer.");
}

static async Task CheckFailedBranchAsync(bool emptyFinding = false)
{
    int composerCalls = 0;
    AIAgent technical = FakeAgent("Technical", _ => Task.FromResult("TECH_FINDING"));
    AIAgent incident = FakeAgent("Incident", _ => emptyFinding
        ? Task.FromResult(" \t")
        : throw new InvalidOperationException("Simulated incident failure."));
    AIAgent editor = FakeAgent("Compose", _ =>
    {
        composerCalls++;
        return Task.FromResult("This must not be produced.");
    });
    SupportComposerExecutor composer = new(editor, ["Technical", "Incident"], "Question");
    var workflow = ConcurrentSupportWorkflow.Build(
        [technical.BindAsExecutor(new AIAgentHostOptions { ForwardIncomingMessages = false }),
         incident.BindAsExecutor(new AIAgentHostOptions { ForwardIncomingMessages = false })], composer);
    await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, new ChatMessage(ChatRole.User, "Question"));
    Assert(await run.TrySendMessageAsync(new TurnToken(emitEvents: true)), "Accept the failure-case processing token.");
    List<WorkflowEvent> events = [];
    await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        events.Add(evt);
    Assert(events.Any(evt => evt is WorkflowErrorEvent or ExecutorFailedEvent), "Surface a failed or empty branch.");
    Assert(composerCalls == 0 && !events.OfType<WorkflowOutputEvent>().Any(evt => evt.Data is string),
        "A failed branch must not become a partial successful answer.");
}

static AIAgent FakeAgent(string name, Func<IReadOnlyList<ChatMessage>, Task<string>> respond) =>
    new ChatClientAgent(new FakeChatClient(respond), name: name, instructions: $"Offline {name} fixture.");

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

// Exercise the real Agent Framework agent bindings, barrier and stream using a
// deterministic model substitute; no Azure credentials, sockets or model prose.
internal sealed class FakeChatClient(Func<IReadOnlyList<ChatMessage>, Task<string>> respond) : IChatClient
{
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        new(new ChatMessage(ChatRole.Assistant, await respond(messages.ToArray())));

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new ChatResponseUpdate(ChatRole.Assistant, await respond(messages.ToArray()));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() { }
}

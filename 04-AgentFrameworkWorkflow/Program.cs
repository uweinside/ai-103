using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

// -----------------------------------------------------------------------------
// LESSON 4: CODE-DEFINED AGENTS + EXPLICIT WORKFLOW ORCHESTRATION
// -----------------------------------------------------------------------------
// App 03 persisted a managed agent version in Foundry. This example instead
// DEFINES agents as C# objects using Microsoft Agent Framework, backed by a
// Foundry model deployment. These are code-defined agents. We then define a
// workflow graph of agents, with explicit directed edges.
//
// No agent magically delegates to another agent because of its instructions.
// WorkflowBuilder declares the topology; InProcessExecution runs that topology.
// The LLM supplies text in each step; the orchestration code decides who runs
// next. This is the key distinction between autonomy and workflow control.
// -----------------------------------------------------------------------------

string endpoint = Required("FOUNDRY_PROJECT_ENDPOINT");
string model = Required("FOUNDRY_MODEL");
// The application supplies the identity; defining three agents does not create
// three Azure identities or grant permissions through model instructions.
// DefaultAzureCredential can use az login locally; the example .env selects
// AzureCliCredential so another local sign-in cannot take precedence. The SDK
// handles token acquisition/refresh, while Azure RBAC controls deployment access.
// Authentication errors propagate without an API-key fallback or token logging.
DefaultAzureCredential credential = new();
var project = new AIProjectClient(new Uri(endpoint), credential);

// The same underlying Foundry project/deployment backs all specialists.
// AsAIAgent(model:, instructions:) creates code-defined abstractions and does
// NOT call AgentAdministrationClient.CreateAgentVersionAsync.
AIAgent triage = project.AsAIAgent(
    model: model,
    name: "TriageAgent",
    instructions: "You are an IT support triage specialist. Given the user's issue, summarize the symptoms and give a cautious diagnosis. Do not invent company policies or claim to take actions. Pass an actionable summary forward.");

AIAgent reviewer = project.AsAIAgent(
    model: model,
    name: "SafetyReviewer",
    instructions: "You review an IT support recommendation for unjustified assumptions, security risks, missing context, and unsafe actions. Provide concrete improvements and do not claim to access real systems.");

AIAgent editor = project.AsAIAgent(
    model: model,
    name: "FinalResponder",
    instructions: "You are the final IT help-desk writer. Turn the reviewed input into a concise helpful answer with safe steps and limitations. Do not claim that anything was performed.");

// This is a SEQUENTIAL graph. You can replace the edges later with parallel,
// branching, handoff, human-in-the-loop or Magentic patterns. Do not confuse
// three separate roles with three separately fine-tuned models: all can share
// the same base deployment but have DIFFERENT instructions and lifecycle.
var workflow = new WorkflowBuilder(triage)
    .AddEdge(triage, reviewer)
    .AddEdge(reviewer, editor)
    .Build();

Console.WriteLine("04 — AGENT FRAMEWORK SEQUENTIAL WORKFLOW");
Console.WriteLine("TriageAgent -> SafetyReviewer -> FinalResponder");
Console.WriteLine("Observe streamed updates: they expose WHICH executor authored them.\n");

var initialMessage = new ChatMessage(ChatRole.User,
    "My laptop VPN fails after a password change. Give me safe first steps without accessing internal systems.");

// In current Agent Framework workflows, agents can wait for a TurnToken even
// after an initial message has been enqueued. The token triggers processing.
// We stream events to demonstrate the boundaries between executors.
await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, initialMessage);
await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    // This event is a model-generated update from one of the agent executors.
    // By printing the executor ID we can show who is doing work at each stage.
    if (evt is AgentResponseUpdateEvent update)
        Console.WriteLine($"[{update.ExecutorId}] {update.Data}");
}

Console.WriteLine("\nDiscuss: Which steps were LLM-generated? Which steps were fixed by WorkflowBuilder?");
Console.WriteLine("Next: add branching, an approval checkpoint, tracing or persistent conversation state.");

static string Required(string n) => Environment.GetEnvironmentVariable(n) is { Length: > 0 } value
    ? value : throw new InvalidOperationException($"Missing {n}; see SETUP.md.");

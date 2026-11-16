using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;

// -----------------------------------------------------------------------------
// LESSON 3: A PERSISTED FOUNDRY AGENT IS DIFFERENT FROM A MODEL REQUEST
// -----------------------------------------------------------------------------
// Lesson 1 made requests directly to a model. Here we create a DECLARATIVE,
// VERSIONED AGENT DEFINITION in Foundry. The instructions and model reference
// exist as an asset that can be inspected and managed in the portal.
//
// AIProjectClient = project-scoped management and runtime client.
// AgentAdministrationClient = persisted definition/version lifecycle.
// AsAIAgent(version) = treat that managed definition as an AIAgent for execution.
//
// This version includes only instructions and a model. Foundry IQ, external
// tools, identities, approvals and production deployment are follow-on topics.
// -----------------------------------------------------------------------------

string endpoint = Required("FOUNDRY_PROJECT_ENDPOINT");
string deployment = Required("FOUNDRY_MODEL");
// The client application authenticates with Entra ID, not the managed agent's
// instructions. DefaultAzureCredential can use az login for local development;
// the SDK requests and refreshes tokens for the appropriate service audience.
// RBAC must also permit agent management and model execution. Authentication
// errors propagate; there is no API-key fallback and credentials are not logged.
DefaultAzureCredential credential = new();
var project = new AIProjectClient(new Uri(endpoint), credential);

// Different executions create unique names to avoid interfering with previous
// classes/instructors. You will see the name in the Foundry portal.
string name = "ai103-helpdesk-" + Guid.NewGuid().ToString("N")[..8];
Console.WriteLine("03 — FOUNDRY-MANAGED AGENT");
Console.WriteLine($"Creating agent definition: {name}");

// The CreateAgentVersionAsync call is a CLOUD MANAGEMENT operation; it is NOT
// the same thing as prompting an LLM. This persists an agent *version*.
ProjectsAgentVersion version = await project.AgentAdministrationClient.CreateAgentVersionAsync(
    name,
    new ProjectsAgentVersionCreationOptions(
        new DeclarativeAgentDefinition(deployment)
        {
            Instructions = "You are a careful corporate IT help-desk assistant. You can explain general troubleshooting steps, but cannot access internal policies or tickets and must not invent them. Ask one clarifying question if needed."
        }));
Console.WriteLine($"Created version {version.Version}. Open Foundry portal to inspect it.\n");

bool keep = string.Equals(Environment.GetEnvironmentVariable("KEEP_DEMO_AGENT"), "true", StringComparison.OrdinalIgnoreCase);
try
{
    // This converts the remote, versioned Foundry definition to the common
    // Agent Framework abstraction AIAgent. AIAgent doesn't imply that the agent
    // definition must be persisted: app 04 will create code-defined agents.
    AIAgent agent = project.AsAIAgent(version);

    string query = "A colleague cannot connect to the company VPN. Suggest safe troubleshooting steps and explain what you cannot verify.";
    Console.WriteLine("Prompt > " + query);
    Console.WriteLine("Agent > " + await agent.RunAsync(query));

    Console.WriteLine("\nCompare three separate things:");
    Console.WriteLine("1. The managed definition/version in Foundry");
    Console.WriteLine("2. The model that generated the text");
    Console.WriteLine("3. The client-side console app that invoked the managed agent");
}
finally
{
    if (keep)
    {
        Console.WriteLine($"\nKEEP_DEMO_AGENT=true; leaving {name} version {version.Version} in Foundry.");
    }
    else
    {
        // Avoid accumulation of classroom assets. Deleting the created agent
        // is safe here because its name is unique to this demo execution.
        await project.AgentAdministrationClient.DeleteAgentAsync(name);
        Console.WriteLine($"\nDeleted temporary demo agent {name}.");
    }
}
static string Required(string n) => Environment.GetEnvironmentVariable(n) is { Length: > 0 } value
    ? value : throw new InvalidOperationException($"Missing {n}; see SETUP.md.");

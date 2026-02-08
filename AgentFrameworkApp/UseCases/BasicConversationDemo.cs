using Azure.AI.Agents.Persistent;

namespace AgentFrameworkApp.UseCases;

/// <summary>
/// Use Case 1: Basic Conversational Agent
/// Demonstrates creating a simple agent that answers general questions 
/// with a custom system prompt (persona).
/// </summary>
public static class BasicConversationDemo
{
    public static async Task RunAsync(PersistentAgentsClient client, string modelName)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║  USE CASE 1: Basic Conversational Agent              ║");
        Console.WriteLine("║  A helpful assistant with a custom persona.          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝\n");

        // Create agent with a specific persona
        PersistentAgent agent = await client.Administration.CreateAgentAsync(
            model: modelName,
            name: "TechAdvisor",
            instructions: """
                You are TechAdvisor, a friendly and knowledgeable technology consultant.
                You specialize in helping people understand modern software architecture,
                cloud computing, and AI concepts in simple terms.
                Keep responses concise (2-3 paragraphs max).
                Use analogies to explain complex topics when possible.
                """
        );

        Console.WriteLine($"Agent created: {agent.Name} (ID: {agent.Id})");

        PersistentAgentThread? thread = null;
        try
        {
            // Turn 1: Ask a question
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "What is the difference between microservices and monolithic architecture?");

            // Turn 2: Follow-up (demonstrates conversation memory)
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "Which one would you recommend for a startup building an MVP?");

            // Turn 3: Different topic
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "Can you explain what Azure AI Foundry is in one paragraph?");
        }
        finally
        {
            await Services.AgentHelper.CleanupAsync(client, agent.Id, thread?.Id);
            Console.WriteLine("\n[Cleanup complete]");
        }
    }
}

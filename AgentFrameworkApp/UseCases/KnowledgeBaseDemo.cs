using Azure.AI.Agents.Persistent;

namespace AgentFrameworkApp.UseCases;

/// <summary>
/// Use Case 4: RAG-style Knowledge Base Agent
/// Demonstrates a Retrieval-Augmented Generation pattern using function tools
/// to query a simulated knowledge base before answering.
/// </summary>
public static class KnowledgeBaseDemo
{
    public static async Task RunAsync(PersistentAgentsClient client, string modelName)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║  USE CASE 4: Knowledge Base Agent (RAG Pattern)     ║");
        Console.WriteLine("║  Agent retrieves info from a knowledge base first.  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝\n");

        // Define the knowledge base search function
        var searchFunction = new FunctionToolDefinition(
            name: "search_knowledge_base",
            description: "Search the company knowledge base for information about policies, products, pricing, and support topics. Always call this tool before answering customer questions.",
            parameters: BinaryData.FromObjectAsJson(new
            {
                Type = "object",
                Properties = new
                {
                    Query = new { Type = "string", Description = "The search query to look up in the knowledge base" }
                },
                Required = new[] { "query" }
            },
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }));

        PersistentAgent agent = await client.Administration.CreateAgentAsync(
            model: modelName,
            name: "SupportAgent",
            instructions: """
                You are SupportAgent, a customer support representative for a SaaS company.
                
                IMPORTANT RULES:
                1. ALWAYS search the knowledge base before answering any customer question.
                2. Only provide information that comes from the knowledge base results.
                3. If the knowledge base doesn't have relevant information, say "I don't have 
                   information about that. Let me connect you with a human agent."
                4. Be polite, professional, and empathetic.
                5. After answering, ask if there's anything else you can help with.
                """,
            tools: [searchFunction]
        );

        Console.WriteLine($"Agent created: {agent.Name} (ID: {agent.Id})");

        PersistentAgentThread? thread = null;
        try
        {
            // Turn 1: Refund policy question
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "Hi, I'd like to return a product I bought 2 weeks ago. What's your refund policy?");

            // Turn 2: Pricing question
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "What pricing plans do you offer? I need something for a team of about 10 people.");

            // Turn 3: Security question
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "We're in a regulated industry. Can you tell me about your security certifications?");
        }
        finally
        {
            await Services.AgentHelper.CleanupAsync(client, agent.Id, thread?.Id);
            Console.WriteLine("\n[Cleanup complete]");
        }
    }
}

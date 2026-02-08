using System.Text.Json;
using Azure.AI.Agents.Persistent;

namespace AgentFrameworkApp.UseCases;

/// <summary>
/// Use Case 3: Function Calling Agent (Weather & Stocks)
/// Demonstrates custom function tools that let the agent call your application
/// code, simulating a real-world integration pattern.
/// </summary>
public static class FunctionCallingDemo
{
    public static async Task RunAsync(PersistentAgentsClient client, string modelName)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║  USE CASE 3: Function Calling Agent                 ║");
        Console.WriteLine("║  Agent calls your custom functions (weather/stocks). ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝\n");

        // Define function tool schemas
        var weatherFunction = new FunctionToolDefinition(
            name: "get_weather",
            description: "Get the current weather for a given city.",
            parameters: BinaryData.FromObjectAsJson(new
            {
                Type = "object",
                Properties = new
                {
                    City = new { Type = "string", Description = "The city name, e.g., 'Seattle'" }
                },
                Required = new[] { "city" }
            },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

        var stockFunction = new FunctionToolDefinition(
            name: "get_stock_price",
            description: "Get the current stock price for a given ticker symbol.",
            parameters: BinaryData.FromObjectAsJson(new
            {
                Type = "object",
                Properties = new
                {
                    Symbol = new { Type = "string", Description = "The stock ticker symbol, e.g., 'MSFT'" }
                },
                Required = new[] { "symbol" }
            },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

        // Create agent with function tools
        PersistentAgent agent = await client.Administration.CreateAgentAsync(
            model: modelName,
            name: "MarketAssistant",
            instructions: """
                You are MarketAssistant, a helpful assistant that provides weather and stock 
                market information. Use the available tools to fetch real-time data when users 
                ask about weather conditions or stock prices. Present the information in a 
                clear, readable format. If a user asks about multiple cities or stocks, look 
                up each one.
                """,
            tools: [weatherFunction, stockFunction]
        );

        Console.WriteLine($"Agent created: {agent.Name} (ID: {agent.Id})");

        PersistentAgentThread? thread = null;
        try
        {
            // Turn 1: Weather query
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "What's the weather like in Seattle and London today?");

            // Turn 2: Stock query
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "How are MSFT, AAPL, and NVDA doing today?");

            // Turn 3: Combined query
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "I'm planning a trip to Tokyo. What's the weather there? Also, check Tesla's stock price.");
        }
        finally
        {
            await Services.AgentHelper.CleanupAsync(client, agent.Id, thread?.Id);
            Console.WriteLine("\n[Cleanup complete]");
        }
    }
}

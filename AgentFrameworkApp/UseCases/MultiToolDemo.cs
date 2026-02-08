using System.Text.Json;
using Azure.AI.Agents.Persistent;

namespace AgentFrameworkApp.UseCases;

/// <summary>
/// Use Case 5: Multi-Tool Agent
/// Demonstrates an agent that has access to multiple tools simultaneously —
/// code interpreter, function calling (weather, stocks, calculator, knowledge base).
/// The agent decides which tool(s) to use based on the user's question.
/// </summary>
public static class MultiToolDemo
{
    public static async Task RunAsync(PersistentAgentsClient client, string modelName)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║  USE CASE 5: Multi-Tool Agent                       ║");
        Console.WriteLine("║  Agent picks the right tool(s) for each question.   ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝\n");

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Define all function tools
        var weatherFunction = new FunctionToolDefinition(
            name: "get_weather",
            description: "Get the current weather for a given city.",
            parameters: BinaryData.FromObjectAsJson(new
            {
                Type = "object",
                Properties = new { City = new { Type = "string", Description = "The city name" } },
                Required = new[] { "city" }
            }, jsonOptions));

        var stockFunction = new FunctionToolDefinition(
            name: "get_stock_price",
            description: "Get the current stock price for a given ticker symbol.",
            parameters: BinaryData.FromObjectAsJson(new
            {
                Type = "object",
                Properties = new { Symbol = new { Type = "string", Description = "Stock ticker symbol like MSFT" } },
                Required = new[] { "symbol" }
            }, jsonOptions));

        var calcFunction = new FunctionToolDefinition(
            name: "calculate",
            description: "Evaluate a mathematical expression. Supports basic arithmetic.",
            parameters: BinaryData.FromObjectAsJson(new
            {
                Type = "object",
                Properties = new { Expression = new { Type = "string", Description = "Math expression, e.g., '(100*1.08)-50'" } },
                Required = new[] { "expression" }
            }, jsonOptions));

        var searchFunction = new FunctionToolDefinition(
            name: "search_knowledge_base",
            description: "Search company knowledge base for policies and product info.",
            parameters: BinaryData.FromObjectAsJson(new
            {
                Type = "object",
                Properties = new { Query = new { Type = "string", Description = "Search query" } },
                Required = new[] { "query" }
            }, jsonOptions));

        // Create agent with ALL tools
        PersistentAgent agent = await client.Administration.CreateAgentAsync(
            model: modelName,
            name: "UniversalAssistant",
            instructions: """
                You are UniversalAssistant, a versatile AI agent with access to multiple tools:
                - Weather lookup for any city
                - Stock price checker for any ticker
                - Calculator for math expressions
                - Knowledge base for company policies and product info
                - Code interpreter for complex analysis and code execution
                
                Choose the appropriate tool(s) based on the user's request. You may use 
                multiple tools in a single response if needed. Always explain what tools 
                you're using and why.
                """,
            tools: [
                new CodeInterpreterToolDefinition(),
                weatherFunction,
                stockFunction,
                calcFunction,
                searchFunction
            ]
        );

        Console.WriteLine($"Agent created: {agent.Name} (ID: {agent.Id})");

        PersistentAgentThread? thread = null;
        try
        {
            // Turn 1: Needs calculator
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "If I buy 150 shares of a stock at $42.50 each, and the brokerage fee is 0.5%, what's my total cost?");

            // Turn 2: Needs weather + stock
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "I'm heading to New York tomorrow. What's the weather? Also check MSFT and GOOGL stock prices.");

            // Turn 3: Needs knowledge base + code interpreter
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "What are your pricing plans? Also, using code, calculate the annual cost for each plan.");
        }
        finally
        {
            await Services.AgentHelper.CleanupAsync(client, agent.Id, thread?.Id);
            Console.WriteLine("\n[Cleanup complete]");
        }
    }
}

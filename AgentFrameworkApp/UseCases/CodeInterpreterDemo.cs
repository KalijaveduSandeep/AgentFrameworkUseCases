using Azure.AI.Agents.Persistent;

namespace AgentFrameworkApp.UseCases;

/// <summary>
/// Use Case 2: Code Interpreter Agent
/// Demonstrates the built-in Code Interpreter tool which lets the agent 
/// write and execute Python code, perform data analysis, and generate files.
/// </summary>
public static class CodeInterpreterDemo
{
    public static async Task RunAsync(PersistentAgentsClient client, string modelName)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║  USE CASE 2: Code Interpreter Agent                 ║");
        Console.WriteLine("║  Agent can write & execute code for data analysis.  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝\n");

        // Create agent with Code Interpreter tool enabled
        PersistentAgent agent = await client.Administration.CreateAgentAsync(
            model: modelName,
            name: "DataAnalyst",
            instructions: """
                You are DataAnalyst, an expert data scientist.
                When asked to analyze data or perform calculations, use the code interpreter 
                to write and run Python code. Show the code you execute and explain the results.
                Always provide clear, actionable insights from the data.
                """,
            tools: [new CodeInterpreterToolDefinition()]
        );

        Console.WriteLine($"Agent created: {agent.Name} (ID: {agent.Id})");

        PersistentAgentThread? thread = null;
        try
        {
            // Turn 1: Math computation
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "Calculate the first 15 Fibonacci numbers and their golden ratio approximations.");

            // Turn 2: Data analysis
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                """
                Create a dataset of monthly sales for a fictional company over 12 months.
                Calculate: mean, median, standard deviation, and month-over-month growth rate.
                Summarize the findings.
                """);

            // Turn 3: Algorithm
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "Write a Python function that checks if a string is a valid palindrome (ignoring spaces and case). Test it with 5 examples.");
        }
        finally
        {
            await Services.AgentHelper.CleanupAsync(client, agent.Id, thread?.Id);
            Console.WriteLine("\n[Cleanup complete]");
        }
    }
}

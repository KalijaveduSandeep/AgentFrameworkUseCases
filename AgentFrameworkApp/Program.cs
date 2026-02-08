using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Configuration;
using AgentFrameworkApp.Services;
using AgentFrameworkApp.UseCases;

// ─── Configuration ───────────────────────────────────────────────────────────
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

string projectEndpoint = configuration["AzureAI:ConnectionString"]
    ?? throw new InvalidOperationException(
        "Missing 'AzureAI:ConnectionString' in appsettings.json. " +
        "Set it to your Azure AI Foundry project endpoint.");

string modelName = configuration["AzureAI:ModelDeploymentName"] ?? "gpt-4o";

string searchConnectionId = configuration["AzureAISearch:ConnectionId"] ?? "";
string searchIndexName = configuration["AzureAISearch:IndexName"] ?? "";

// ─── Create Client ───────────────────────────────────────────────────────────
PersistentAgentsClient client = AgentClientFactory.GetClient(projectEndpoint);

Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine("  Azure AI Foundry – Agent Framework Demo (.NET)");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine($"  Model: {modelName}");
Console.WriteLine("═══════════════════════════════════════════════════════════\n");

// ─── Menu Loop ───────────────────────────────────────────────────────────────
bool running = true;
while (running)
{
    Console.WriteLine("\nSelect a use case to run:\n");
    Console.WriteLine("  1. Basic Conversational Agent");
    Console.WriteLine("  2. Code Interpreter Agent (data analysis & code execution)");
    Console.WriteLine("  3. Function Calling Agent (weather & stock tools)");
    Console.WriteLine("  4. Knowledge Base Agent (RAG pattern)");
    Console.WriteLine("  5. Multi-Tool Agent (all tools combined)");
    Console.WriteLine("  7. Real RAG Chat with Azure AI Search (interactive)");
    Console.WriteLine("  6. Run ALL demos sequentially");
    Console.WriteLine("  0. Exit\n");
    Console.Write("Enter choice (0-7): ");

    string? choice = Console.ReadLine()?.Trim();

    try
    {
        switch (choice)
        {
            case "1":
                await BasicConversationDemo.RunAsync(client, modelName);
                break;
            case "2":
                await CodeInterpreterDemo.RunAsync(client, modelName);
                break;
            case "3":
                await FunctionCallingDemo.RunAsync(client, modelName);
                break;
            case "4":
                await KnowledgeBaseDemo.RunAsync(client, modelName);
                break;
            case "5":
                await MultiToolDemo.RunAsync(client, modelName);
                break;
            case "7":
                if (string.IsNullOrWhiteSpace(searchConnectionId) || string.IsNullOrWhiteSpace(searchIndexName))
                {
                    Console.WriteLine("[Error]: AzureAISearch:ConnectionId and AzureAISearch:IndexName must be set in appsettings.json.");
                }
                else
                {
                    await AzureAISearchRAGDemo.RunAsync(client, modelName, searchConnectionId, searchIndexName);
                }
                break;
            case "6":
                Console.WriteLine("\n> Running ALL demos...\n");
                await BasicConversationDemo.RunAsync(client, modelName);
                await CodeInterpreterDemo.RunAsync(client, modelName);
                await FunctionCallingDemo.RunAsync(client, modelName);
                await KnowledgeBaseDemo.RunAsync(client, modelName);
                await MultiToolDemo.RunAsync(client, modelName);
                Console.WriteLine("\nAll demos completed!");
                break;
            case "0":
                running = false;
                Console.WriteLine("\nGoodbye!");
                break;
            default:
                Console.WriteLine("Invalid choice. Please enter 0-7.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n[Error]: {ex.Message}");
        if (ex.InnerException is not null)
            Console.WriteLine($"[Inner]: {ex.InnerException.Message}");
        Console.ResetColor();
    }
}

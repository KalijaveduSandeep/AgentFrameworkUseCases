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
    Console.WriteLine("  ── High Priority ────────────────────────────────────");
    Console.WriteLine("   7. Multi-Agent Orchestration (Researcher → Writer)");
    Console.WriteLine("   8. File Search Agent (upload & search documents)");
    Console.WriteLine("   9. Streaming Response Agent (real-time token output)");
    Console.WriteLine("  ── Medium Priority ──────────────────────────────────");
    Console.WriteLine("  10. Image / Vision Agent (analyze images)");
    Console.WriteLine("  11. Conversation Memory & History (resume sessions)");
    Console.WriteLine("  12. Guardrails & Safety Agent (responsible AI)");
    Console.WriteLine("  13. Event-Driven Agent (process emails/alerts)");
    Console.WriteLine("  ── Simple Priority ──────────────────────────────────");
    Console.WriteLine("  14. Structured Output Agent (strict JSON responses)");
    Console.WriteLine("  15. Error Handling & Retry Patterns (resilience)");
    Console.WriteLine("  ── Original Demos ───────────────────────────────────");
    Console.WriteLine("   1. Basic Conversational Agent");
    Console.WriteLine("   2. Code Interpreter Agent (data analysis & code execution)");
    Console.WriteLine("   3. Function Calling Agent (weather & stock tools)");
    Console.WriteLine("   4. Knowledge Base Agent (RAG pattern)");
    Console.WriteLine("   5. Multi-Tool Agent (all tools combined)");
    Console.WriteLine("   6. Real RAG Chat with Azure AI Search (interactive)");
    Console.WriteLine("  ─────────────────────────────────────────────────────");
    Console.WriteLine("  99. Run ALL demos sequentially");
    Console.WriteLine("   0. Exit\n");
    Console.Write("Enter choice (0-15 or 99): ");

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
            case "6":
                if (string.IsNullOrWhiteSpace(searchConnectionId) || string.IsNullOrWhiteSpace(searchIndexName))
                {
                    Console.WriteLine("[Error]: AzureAISearch:ConnectionId and AzureAISearch:IndexName must be set in appsettings.json.");
                }
                else
                {
                    await AzureAISearchRAGDemo.RunAsync(client, modelName, searchConnectionId, searchIndexName);
                }
                break;
            // ── High Priority ────────────────────────────────────
            case "7":
                await MultiAgentOrchestrationDemo.RunAsync(client, modelName);
                break;
            case "8":
                await FileSearchDemo.RunAsync(client, modelName);
                break;
            case "9":
                await StreamingResponseDemo.RunAsync(client, modelName);
                break;
            // ── Medium Priority ──────────────────────────────────
            case "10":
                await ImageVisionDemo.RunAsync(client, modelName);
                break;
            case "11":
                await ConversationMemoryDemo.RunAsync(client, modelName);
                break;
            case "12":
                await GuardrailsSafetyDemo.RunAsync(client, modelName);
                break;
            case "13":
                await EventDrivenAgentDemo.RunAsync(client, modelName);
                break;
            // ── Simple Priority ──────────────────────────────────
            case "14":
                await StructuredOutputDemo.RunAsync(client, modelName);
                break;
            case "15":
                await ErrorHandlingRetryDemo.RunAsync(client, modelName);
                break;
            case "99":
                Console.WriteLine("\n> Running ALL demos...\n");
                await BasicConversationDemo.RunAsync(client, modelName);
                await CodeInterpreterDemo.RunAsync(client, modelName);
                await FunctionCallingDemo.RunAsync(client, modelName);
                await KnowledgeBaseDemo.RunAsync(client, modelName);
                await MultiToolDemo.RunAsync(client, modelName);
                await MultiAgentOrchestrationDemo.RunAsync(client, modelName);
                await FileSearchDemo.RunAsync(client, modelName);
                await StreamingResponseDemo.RunAsync(client, modelName);
                await ImageVisionDemo.RunAsync(client, modelName);
                await GuardrailsSafetyDemo.RunAsync(client, modelName);
                await EventDrivenAgentDemo.RunAsync(client, modelName);
                await StructuredOutputDemo.RunAsync(client, modelName);
                await ErrorHandlingRetryDemo.RunAsync(client, modelName);
                Console.WriteLine("\nAll demos completed!");
                break;
            case "0":
                running = false;
                Console.WriteLine("\nGoodbye!");
                break;
            default:
                Console.WriteLine("Invalid choice. Please enter 0-15 or 99.");
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

using System.Text.Json;
using Azure.AI.Agents.Persistent;

namespace AgentFrameworkApp.UseCases;

/// <summary>
/// Use Case 16: Error Handling & Retry Patterns
/// Demonstrates production-grade error handling — exponential backoff,
/// graceful degradation, timeout handling, and run failure recovery.
/// </summary>
public static class ErrorHandlingRetryDemo
{
    public static async Task RunAsync(PersistentAgentsClient client, string modelName)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  USE CASE 16: Error Handling & Retry Patterns           ║");
        Console.WriteLine("║  Production-grade resilience: retries, timeouts,         ║");
        Console.WriteLine("║  graceful degradation, and failure recovery.             ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

        // ─── Example 1: Retry with Exponential Backoff ───────────────────────
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("── Example 1: Conversation with Retry Logic ─────────────");
        Console.ResetColor();

        PersistentAgent? agent = null;
        PersistentAgentThread? thread = null;

        try
        {
            // Demonstrate resilient agent creation with retry
            agent = await RetryAsync(
                operation: () => client.Administration.CreateAgentAsync(
                    model: modelName,
                    name: "ResilientAssistant",
                    instructions: """
                        You are ResilientAssistant, a helpful AI.
                        Provide clear, concise answers. If asked about error handling,
                        give practical examples with C# or Python code.
                        """),
                operationName: "Create Agent",
                maxRetries: 3);

            Console.WriteLine($"Agent created: {agent.Name} (ID: {agent.Id})");

            // Demonstrate resilient conversation turn
            thread = await client.Threads.CreateThreadAsync();

            string[] questions =
            [
                "What are the best practices for error handling in distributed systems?",
                "Show me a C# example of implementing the circuit breaker pattern.",
                "How should I handle transient failures in Azure service calls?"
            ];

            foreach (string question in questions)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n[You]: {question}");
                Console.ResetColor();

                var response = await RunWithTimeoutAndRetryAsync(
                    client, agent, thread, question,
                    timeout: TimeSpan.FromSeconds(60),
                    maxRetries: 2);

                if (response is not null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n[Agent]: {response}");
                    Console.ResetColor();
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[Unrecoverable error]: {ex.Message}");
            Console.ResetColor();
        }
        finally
        {
            if (agent is not null)
                await SafeCleanupAsync(client, agent.Id, thread?.Id);
            Console.WriteLine();
        }

        // ─── Example 2: Handling Invalid Tool Calls Gracefully ───────────────
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("── Example 2: Graceful Tool Call Error Handling ─────────");
        Console.ResetColor();

        PersistentAgent? toolAgent = null;
        PersistentAgentThread? toolThread = null;

        try
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            var riskyFunction = new FunctionToolDefinition(
                name: "get_database_record",
                description: "Retrieve a record from the database by ID.",
                parameters: BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        RecordId = new { Type = "string", Description = "The record ID to look up" },
                        Table = new { Type = "string", Description = "The database table name" }
                    },
                    Required = new[] { "recordId", "table" }
                }, jsonOptions));

            toolAgent = await client.Administration.CreateAgentAsync(
                model: modelName,
                name: "DatabaseAssistant",
                instructions: """
                    You help users query a database. Use the get_database_record tool 
                    to look up records. Ask for clarification if the user's request is vague.
                    """,
                tools: [riskyFunction]);

            Console.WriteLine($"Agent created: {toolAgent.Name}");

            toolThread = await client.Threads.CreateThreadAsync();
            await client.Messages.CreateMessageAsync(
                toolThread.Id, MessageRole.User,
                "Look up employee record EMP-001 from the employees table, then also find order ORD-555 from the orders table.");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[You]: Look up employee EMP-001 and order ORD-555");
            Console.ResetColor();

            ThreadRun run = await client.Runs.CreateRunAsync(toolThread, toolAgent);
            int toolCallAttempts = 0;
            const int maxToolRetries = 5;

            do
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                run = await client.Runs.GetRunAsync(toolThread.Id, run.Id);

                if (run.Status == RunStatus.RequiresAction
                    && run.RequiredAction is SubmitToolOutputsAction submitAction)
                {
                    toolCallAttempts++;
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"  [Tool calls received — attempt {toolCallAttempts}/{maxToolRetries}]");
                    Console.ResetColor();

                    var toolOutputs = new List<ToolOutput>();

                    foreach (RequiredToolCall toolCall in submitAction.ToolCalls)
                    {
                        if (toolCall is RequiredFunctionToolCall funcCall)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.WriteLine($"  [Tool call]: {funcCall.Name}({funcCall.Arguments})");
                            Console.ResetColor();

                            // Simulate tool execution with error handling
                            string output = ExecuteToolSafely(funcCall.Name, funcCall.Arguments);
                            toolOutputs.Add(new ToolOutput(toolCall, output));
                        }
                    }

                    run = await client.Runs.SubmitToolOutputsToRunAsync(run, toolOutputs, toolApprovals: null);

                    if (toolCallAttempts >= maxToolRetries)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("  [Max tool call attempts reached — stopping]");
                        Console.ResetColor();
                        break;
                    }
                }
            }
            while (run.Status == RunStatus.Queued
                || run.Status == RunStatus.InProgress
                || run.Status == RunStatus.RequiresAction);

            // Display result
            if (run.Status == RunStatus.Completed)
            {
                await foreach (PersistentThreadMessage msg in client.Messages.GetMessagesAsync(
                    threadId: toolThread.Id, order: ListSortOrder.Descending))
                {
                    if (msg.Role == MessageRole.Agent)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        foreach (MessageContent content in msg.ContentItems)
                        {
                            if (content is MessageTextContent textContent)
                                Console.WriteLine($"\n[Agent]: {textContent.Text}");
                        }
                        Console.ResetColor();
                        break;
                    }
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Run ended with status: {run.Status}]");
                if (run.Status == RunStatus.Failed)
                    Console.WriteLine($"[Error]: {run.LastError?.Message}");
                Console.ResetColor();
            }
        }
        finally
        {
            if (toolAgent is not null)
                await SafeCleanupAsync(client, toolAgent.Id, toolThread?.Id);
        }

        // ─── Summary ────────────────────────────────────────────────────────
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n── ERROR HANDLING PATTERNS DEMONSTRATED ──────────────");
        Console.WriteLine("  1. Exponential backoff retry for API calls");
        Console.WriteLine("  2. Timeout handling for long-running operations");
        Console.WriteLine("  3. Safe tool execution with try-catch wrappers");
        Console.WriteLine("  4. Graceful resource cleanup in finally blocks");
        Console.WriteLine("  5. Max retry limits to prevent infinite loops");
        Console.ResetColor();
        Console.WriteLine("\n[Cleanup complete]");
    }

    /// <summary>
    /// Generic retry with exponential backoff.
    /// </summary>
    private static async Task<T> RetryAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        int maxRetries = 3,
        int baseDelayMs = 1000)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                int delay = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"  [Retry {attempt}/{maxRetries}] {operationName} failed: {ex.Message}");
                Console.WriteLine($"  Waiting {delay}ms before retry...");
                Console.ResetColor();
                await Task.Delay(delay);
            }
        }

        // Final attempt — let exception propagate
        return await operation();
    }

    /// <summary>
    /// Run a conversation turn with timeout and retry logic.
    /// </summary>
    private static async Task<string?> RunWithTimeoutAndRetryAsync(
        PersistentAgentsClient client,
        PersistentAgent agent,
        PersistentAgentThread thread,
        string userMessage,
        TimeSpan timeout,
        int maxRetries = 2)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await client.Messages.CreateMessageAsync(thread.Id, MessageRole.User, userMessage);
                ThreadRun run = await client.Runs.CreateRunAsync(thread, agent);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("  Processing");

                do
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    run = await client.Runs.GetRunAsync(thread.Id, run.Id);
                    Console.Write(".");

                    if (stopwatch.Elapsed > timeout)
                    {
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"\n  [Timeout after {timeout.TotalSeconds}s — attempt {attempt}/{maxRetries}]");
                        Console.ResetColor();

                        // Try to cancel the run
                        try { await client.Runs.CancelRunAsync(thread.Id, run.Id); }
                        catch { /* best effort */ }

                        throw new TimeoutException($"Run did not complete within {timeout.TotalSeconds}s");
                    }
                }
                while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);

                Console.ResetColor();
                Console.Write($" ({stopwatch.ElapsedMilliseconds}ms)");
                Console.WriteLine();

                if (run.Status == RunStatus.Failed)
                {
                    throw new Exception($"Run failed: {run.LastError?.Message}");
                }

                // Extract response
                await foreach (PersistentThreadMessage msg in client.Messages.GetMessagesAsync(
                    threadId: thread.Id, order: ListSortOrder.Descending))
                {
                    if (msg.Role == MessageRole.Agent)
                    {
                        foreach (MessageContent content in msg.ContentItems)
                        {
                            if (content is MessageTextContent textContent)
                                return textContent.Text;
                        }
                    }
                }

                return "[No response received]";
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                int delay = 1000 * (int)Math.Pow(2, attempt - 1);
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"  [Retry {attempt}/{maxRetries}]: {ex.Message}");
                Console.WriteLine($"  Retrying in {delay}ms...");
                Console.ResetColor();
                await Task.Delay(delay);
            }
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("  [All retries exhausted — returning fallback response]");
        Console.ResetColor();
        return "[Service temporarily unavailable. Please try again later.]";
    }

    /// <summary>
    /// Executes a tool call with error handling — returns error JSON instead of throwing.
    /// </summary>
    private static string ExecuteToolSafely(string functionName, string argumentsJson)
    {
        try
        {
            var args = JsonDocument.Parse(argumentsJson);

            return functionName switch
            {
                "get_database_record" => SimulateDatabaseLookup(args),
                _ => JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" })
            };
        }
        catch (JsonException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [Tool error]: Invalid JSON arguments — {ex.Message}");
            Console.ResetColor();
            return JsonSerializer.Serialize(new { error = "Invalid argument format", details = ex.Message });
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [Tool error]: {ex.Message}");
            Console.ResetColor();
            return JsonSerializer.Serialize(new { error = "Tool execution failed", details = ex.Message });
        }
    }

    private static string SimulateDatabaseLookup(JsonDocument args)
    {
        string recordId = args.RootElement.TryGetProperty("recordId", out var rid)
            ? rid.GetString() ?? "unknown" : "unknown";
        string table = args.RootElement.TryGetProperty("table", out var tbl)
            ? tbl.GetString() ?? "unknown" : "unknown";

        // Simulate different outcomes
        return (table.ToLower(), recordId.ToUpper()) switch
        {
            ("employees", "EMP-001") => JsonSerializer.Serialize(new
            {
                recordId,
                table,
                data = new { Name = "Alice Johnson", Department = "Engineering", Role = "Senior Developer", StartDate = "2021-03-15" }
            }),
            ("orders", "ORD-555") => JsonSerializer.Serialize(new
            {
                recordId,
                table,
                data = new { Product = "SmartWidget Pro (x10)", Total = "$2,990.00", Status = "Shipped", Date = "2025-12-01" }
            }),
            _ => JsonSerializer.Serialize(new
            {
                recordId,
                table,
                error = $"Record '{recordId}' not found in table '{table}'",
                suggestion = "Check the record ID and table name"
            })
        };
    }

    /// <summary>
    /// Safely cleans up resources, catching and logging any errors.
    /// </summary>
    private static async Task SafeCleanupAsync(PersistentAgentsClient client, string agentId, string? threadId)
    {
        try
        {
            if (threadId is not null)
                await client.Threads.DeleteThreadAsync(threadId);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"  [Cleanup warning — thread]: {ex.Message}");
            Console.ResetColor();
        }

        try
        {
            await client.Administration.DeleteAgentAsync(agentId);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"  [Cleanup warning — agent]: {ex.Message}");
            Console.ResetColor();
        }
    }
}

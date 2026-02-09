using System.Text.Json;
using Azure.AI.Agents.Persistent;

namespace AgentFrameworkApp.UseCases;

/// <summary>
/// Use Case 12: Conversation with Memory & History
/// Demonstrates persisting thread IDs so users can resume previous conversations.
/// Shows how to list message history from an existing thread and continue chatting.
/// </summary>
public static class ConversationMemoryDemo
{
    private static readonly string HistoryFile = Path.Combine(
        AppContext.BaseDirectory, "conversation_history.json");

    public static async Task RunAsync(PersistentAgentsClient client, string modelName)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  USE CASE 12: Conversation Memory & History             ║");
        Console.WriteLine("║  Resume past conversations using persisted thread IDs.  ║");
        Console.WriteLine("║  Type 'exit' to end  |  'history' to view past messages ║");
        Console.WriteLine("║  'new' for a new conversation  |  'list' for threads    ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

        PersistentAgent agent = await client.Administration.CreateAgentAsync(
            model: modelName,
            name: "MemoryAssistant",
            instructions: """
                You are MemoryAssistant, a helpful AI that maintains conversation context.
                You remember everything discussed in the current thread.
                
                When a user returns to a conversation:
                1. Acknowledge that you remember the previous context.
                2. Reference specific details from earlier in the conversation.
                3. Build upon previous discussions naturally.
                
                Be conversational and personable. Remember user preferences and details 
                they've shared.
                """
        );

        Console.WriteLine($"Agent created: {agent.Name} (ID: {agent.Id})");

        // Load saved conversations
        var savedThreads = LoadSavedThreads();

        PersistentAgentThread? thread = null;
        string? activeThreadId = null;

        try
        {
            // Check if there are saved conversations to resume
            if (savedThreads.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\nFound {savedThreads.Count} saved conversation(s):");
                for (int i = 0; i < savedThreads.Count; i++)
                {
                    Console.WriteLine($"  [{i + 1}] Thread: {savedThreads[i].ThreadId} — {savedThreads[i].Topic} ({savedThreads[i].Timestamp})");
                }
                Console.ResetColor();

                Console.Write("\nResume a conversation (enter number) or press Enter for new: ");
                string? resumeChoice = Console.ReadLine()?.Trim();

                if (int.TryParse(resumeChoice, out int idx) && idx >= 1 && idx <= savedThreads.Count)
                {
                    activeThreadId = savedThreads[idx - 1].ThreadId;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\nResuming conversation: {activeThreadId}");
                    Console.ResetColor();

                    // Display recent history
                    await DisplayHistoryAsync(client, activeThreadId, maxMessages: 6);
                }
            }

            // Create new thread if not resuming
            if (activeThreadId is null)
            {
                thread = await client.Threads.CreateThreadAsync();
                activeThreadId = thread.Id;
                Console.WriteLine($"\nStarted new conversation: {activeThreadId}");
            }

            Console.WriteLine("─────────────────────────────────────────────────────────\n");

            // Interactive chat loop
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("[You]: ");
                Console.ResetColor();

                string? input = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(input)) continue;

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    // Save thread before exiting
                    Console.Write("Save this conversation? Topic/label (or Enter to skip): ");
                    string? topic = Console.ReadLine()?.Trim();
                    if (!string.IsNullOrWhiteSpace(topic))
                    {
                        savedThreads.Add(new SavedThread(activeThreadId, topic, DateTime.Now.ToString("yyyy-MM-dd HH:mm")));
                        SaveThreads(savedThreads);
                        Console.WriteLine($"Conversation saved with topic: {topic}");
                    }
                    Console.WriteLine("\nEnding session.");
                    break;
                }

                if (input.Equals("history", StringComparison.OrdinalIgnoreCase))
                {
                    await DisplayHistoryAsync(client, activeThreadId, maxMessages: 20);
                    continue;
                }

                if (input.Equals("new", StringComparison.OrdinalIgnoreCase))
                {
                    thread = await client.Threads.CreateThreadAsync();
                    activeThreadId = thread.Id;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Started new conversation: {activeThreadId}");
                    Console.ResetColor();
                    continue;
                }

                if (input.Equals("list", StringComparison.OrdinalIgnoreCase))
                {
                    if (savedThreads.Count == 0)
                    {
                        Console.WriteLine("No saved conversations.");
                    }
                    else
                    {
                        foreach (var st in savedThreads)
                            Console.WriteLine($"  • {st.ThreadId} — {st.Topic} ({st.Timestamp})");
                    }
                    continue;
                }

                // Send message and get response
                await client.Messages.CreateMessageAsync(activeThreadId, MessageRole.User, input);
                ThreadRun run = await client.Runs.CreateRunAsync(activeThreadId, agent.Id);

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("  Thinking");
                do
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    run = await client.Runs.GetRunAsync(activeThreadId, run.Id);
                    Console.Write(".");
                }
                while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);
                Console.ResetColor();
                Console.WriteLine();

                if (run.Status == RunStatus.Failed)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Error]: {run.LastError?.Message}");
                    Console.ResetColor();
                    continue;
                }

                // Display response
                await foreach (PersistentThreadMessage msg in client.Messages.GetMessagesAsync(
                    threadId: activeThreadId, order: ListSortOrder.Descending))
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
                Console.WriteLine();
            }
        }
        finally
        {
            await client.Administration.DeleteAgentAsync(agent.Id);
            // Note: We intentionally do NOT delete the thread so it can be resumed later
            Console.WriteLine("[Agent deleted — thread preserved for future sessions]");
        }
    }

    private static async Task DisplayHistoryAsync(PersistentAgentsClient client, string threadId, int maxMessages)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n── Conversation History ───────────────────────────────");
        Console.ResetColor();

        var messages = new List<(string Role, string Text)>();

        try
        {
            await foreach (PersistentThreadMessage msg in client.Messages.GetMessagesAsync(
                threadId: threadId, order: ListSortOrder.Ascending))
            {
                foreach (MessageContent content in msg.ContentItems)
                {
                    if (content is MessageTextContent textContent)
                    {
                        string role = msg.Role == MessageRole.User ? "You" : "Agent";
                        messages.Add((role, textContent.Text));
                    }
                }
            }

            // Show last N messages
            var recent = messages.Count > maxMessages
                ? messages.Skip(messages.Count - maxMessages).ToList()
                : messages;

            if (messages.Count > maxMessages)
                Console.WriteLine($"  (showing last {maxMessages} of {messages.Count} messages)\n");

            foreach (var (role, text) in recent)
            {
                Console.ForegroundColor = role == "You" ? ConsoleColor.Cyan : ConsoleColor.Green;
                Console.WriteLine($"[{role}]: {text}");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Could not load history]: {ex.Message}");
            Console.ResetColor();
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("───────────────────────────────────────────────────────\n");
        Console.ResetColor();
    }

    private static List<SavedThread> LoadSavedThreads()
    {
        try
        {
            if (File.Exists(HistoryFile))
            {
                string json = File.ReadAllText(HistoryFile);
                return JsonSerializer.Deserialize<List<SavedThread>>(json) ?? [];
            }
        }
        catch { /* ignore corrupt file */ }
        return [];
    }

    private static void SaveThreads(List<SavedThread> threads)
    {
        File.WriteAllText(HistoryFile, JsonSerializer.Serialize(threads,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private record SavedThread(string ThreadId, string Topic, string Timestamp);
}

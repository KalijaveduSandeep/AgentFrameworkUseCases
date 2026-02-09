using Azure.AI.Agents.Persistent;

namespace AgentFrameworkApp.UseCases;

/// <summary>
/// Use Case 8: Multi-Agent Orchestration
/// Demonstrates two agents collaborating — a Researcher gathers information
/// and a Writer takes that output and produces a polished summary.
/// This shows the Workflow Orchestration pattern.
/// </summary>
public static class MultiAgentOrchestrationDemo
{
    public static async Task RunAsync(PersistentAgentsClient client, string modelName)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  USE CASE 8: Multi-Agent Orchestration                  ║");
        Console.WriteLine("║  A Researcher agent gathers info, then a Writer agent   ║");
        Console.WriteLine("║  produces a polished summary from the research.         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

        // ─── Agent 1: Researcher ─────────────────────────────────────────────
        PersistentAgent researcher = await client.Administration.CreateAgentAsync(
            model: modelName,
            name: "Researcher",
            instructions: """
                You are Researcher, a thorough and analytical research assistant.
                When given a topic, provide a detailed, fact-based research brief covering:
                1. Key concepts and definitions
                2. Current trends and developments
                3. Advantages and disadvantages
                4. Real-world examples or use cases
                5. Important statistics or data points (use realistic estimates)
                
                Output your findings as structured bullet points. Be comprehensive 
                but factual — do not add opinions. This research will be handed off 
                to another agent for writing.
                """
        );

        // ─── Agent 2: Writer ─────────────────────────────────────────────────
        PersistentAgent writer = await client.Administration.CreateAgentAsync(
            model: modelName,
            name: "Writer",
            instructions: """
                You are Writer, a skilled content writer who turns research notes into 
                polished, engaging content. You will receive research bullet points from 
                a Researcher agent.

                Your job:
                1. Transform the raw research into a well-structured article/summary.
                2. Add a compelling introduction and conclusion.
                3. Use clear headings and subheadings.
                4. Make the content accessible to a general technical audience.
                5. Keep the total output under 500 words.
                6. Do NOT add information beyond what was researched — stay faithful to the data.
                """
        );

        Console.WriteLine($"Researcher agent created: {researcher.Name} (ID: {researcher.Id})");
        Console.WriteLine($"Writer agent created: {writer.Name} (ID: {writer.Id})");

        PersistentAgentThread? researchThread = null;
        PersistentAgentThread? writerThread = null;

        try
        {
            string topic = "The impact of AI Agents on enterprise software development in 2025-2026";

            // ─── Step 1: Researcher gathers information ──────────────────────
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n── PHASE 1: Research ──────────────────────────────────");
            Console.ResetColor();

            researchThread = await client.Threads.CreateThreadAsync();
            await client.Messages.CreateMessageAsync(
                researchThread.Id, MessageRole.User,
                $"Research the following topic thoroughly: {topic}");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[You → Researcher]: Research: {topic}");
            Console.ResetColor();

            ThreadRun researchRun = await client.Runs.CreateRunAsync(researchThread, researcher);
            researchRun = await PollRunAsync(client, researchThread.Id, researchRun.Id);

            if (researchRun.Status == RunStatus.Failed)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Researcher failed]: {researchRun.LastError?.Message}");
                Console.ResetColor();
                return;
            }

            // Extract research output
            string researchOutput = await GetLatestAgentResponseAsync(client, researchThread.Id);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[Researcher]: {researchOutput}");
            Console.ResetColor();

            // ─── Step 2: Writer produces polished content ────────────────────
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n── PHASE 2: Writing ───────────────────────────────────");
            Console.ResetColor();

            writerThread = await client.Threads.CreateThreadAsync();
            await client.Messages.CreateMessageAsync(
                writerThread.Id, MessageRole.User,
                $"""
                Here are the research notes from our Researcher agent on "{topic}":

                ---
                {researchOutput}
                ---

                Please transform these research notes into a polished, well-structured summary article.
                """);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[You → Writer]: Handed off research notes for polishing...");
            Console.ResetColor();

            ThreadRun writerRun = await client.Runs.CreateRunAsync(writerThread, writer);
            writerRun = await PollRunAsync(client, writerThread.Id, writerRun.Id);

            if (writerRun.Status == RunStatus.Failed)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Writer failed]: {writerRun.LastError?.Message}");
                Console.ResetColor();
                return;
            }

            // Display final polished output
            string finalArticle = await GetLatestAgentResponseAsync(client, writerThread.Id);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[Writer]: {finalArticle}");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n── ORCHESTRATION COMPLETE ─────────────────────────────");
            Console.WriteLine("  Researcher → gathered data → Writer → polished article");
            Console.ResetColor();
        }
        finally
        {
            await Services.AgentHelper.CleanupAsync(client, researcher.Id, researchThread?.Id);
            await Services.AgentHelper.CleanupAsync(client, writer.Id, writerThread?.Id);
            Console.WriteLine("\n[Cleanup complete]");
        }
    }

    private static async Task<ThreadRun> PollRunAsync(PersistentAgentsClient client, string threadId, string runId)
    {
        ThreadRun run;
        do
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            run = await client.Runs.GetRunAsync(threadId, runId);
        }
        while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);
        return run;
    }

    private static async Task<string> GetLatestAgentResponseAsync(PersistentAgentsClient client, string threadId)
    {
        await foreach (PersistentThreadMessage msg in client.Messages.GetMessagesAsync(
            threadId: threadId, order: ListSortOrder.Descending))
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
}

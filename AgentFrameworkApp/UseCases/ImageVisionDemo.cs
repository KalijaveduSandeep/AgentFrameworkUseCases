using Azure.AI.Agents.Persistent;

namespace AgentFrameworkApp.UseCases;

/// <summary>
/// Use Case 11: Image/Vision Agent
/// Demonstrates multi-modal input — sending an image URL to the agent
/// and asking questions about it. The agent analyzes the image and responds.
/// </summary>
public static class ImageVisionDemo
{
    public static async Task RunAsync(PersistentAgentsClient client, string modelName)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  USE CASE 11: Image / Vision Agent                      ║");
        Console.WriteLine("║  Agent analyzes images and answers questions about them. ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

        PersistentAgent agent = await client.Administration.CreateAgentAsync(
            model: modelName,
            name: "VisionAnalyst",
            instructions: """
                You are VisionAnalyst, an expert at analyzing images and visual content.
                
                When the user provides an image:
                1. Describe what you see in detail.
                2. Identify key objects, text, colors, and patterns.
                3. Provide context about what the image represents.
                4. Answer any specific questions the user asks about the image.
                5. If the image is a diagram or chart, interpret the data.
                
                Be precise and observational. If you cannot see an image clearly, say so.
                """
        );

        Console.WriteLine($"Agent created: {agent.Name} (ID: {agent.Id})");
        Console.WriteLine("─────────────────────────────────────────────────────────\n");

        PersistentAgentThread thread = await client.Threads.CreateThreadAsync();

        try
        {
            // ─── Turn 1: Analyze an architecture diagram ─────────────────────
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[You]: Analyze this Azure architecture diagram. What services are shown?");
            Console.ResetColor();

            // Use a publicly available Azure architecture image
            string imageUrl1 = "https://learn.microsoft.com/en-us/azure/architecture/browse/thumbs/basic-web-app.png";
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  (Image: {imageUrl1})");
            Console.ResetColor();

            await client.Messages.CreateMessageAsync(
                thread.Id,
                MessageRole.User,
                [
                    new MessageInputTextBlock("Analyze this Azure architecture diagram. What services are shown and how do they connect?"),
                    new MessageInputImageUriBlock(new MessageImageUriParam(imageUrl1))
                ]);

            ThreadRun run1 = await client.Runs.CreateRunAsync(thread, agent);
            run1 = await PollRunAsync(client, thread.Id, run1.Id);
            await DisplayLatestResponseAsync(client, thread.Id, run1);

            // ─── Turn 2: Analyze a code screenshot ───────────────────────────
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n[You]: What programming language is this? Explain what the code does.");
            Console.ResetColor();

            string imageUrl2 = "https://upload.wikimedia.org/wikipedia/commons/thumb/b/b5/Hello_World_in_Python.png/640px-Hello_World_in_Python.png";
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  (Image: {imageUrl2})");
            Console.ResetColor();

            await client.Messages.CreateMessageAsync(
                thread.Id,
                MessageRole.User,
                [
                    new MessageInputTextBlock("What programming language is this? Explain what the code does."),
                    new MessageInputImageUriBlock(new MessageImageUriParam(imageUrl2))
                ]);

            ThreadRun run2 = await client.Runs.CreateRunAsync(thread, agent);
            run2 = await PollRunAsync(client, thread.Id, run2.Id);
            await DisplayLatestResponseAsync(client, thread.Id, run2);

            // ─── Turn 3: Interactive — user provides their own image URL ─────
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n── Try your own image ─────────────────────────────────");
            Console.ResetColor();
            Console.Write("Paste an image URL (or press Enter to skip): ");
            string? customUrl = Console.ReadLine()?.Trim();

            if (!string.IsNullOrWhiteSpace(customUrl) && Uri.IsWellFormedUriString(customUrl, UriKind.Absolute))
            {
                Console.Write("What would you like to know about this image? ");
                string? userQuestion = Console.ReadLine()?.Trim();

                if (!string.IsNullOrWhiteSpace(userQuestion))
                {
                    await client.Messages.CreateMessageAsync(
                        thread.Id,
                        MessageRole.User,
                        [
                            new MessageInputTextBlock(userQuestion),
                            new MessageInputImageUriBlock(new MessageImageUriParam(customUrl))
                        ]);

                    ThreadRun run3 = await client.Runs.CreateRunAsync(thread, agent);
                    run3 = await PollRunAsync(client, thread.Id, run3.Id);
                    await DisplayLatestResponseAsync(client, thread.Id, run3);
                }
            }
            else
            {
                Console.WriteLine("Skipping custom image analysis.");
            }
        }
        finally
        {
            await Services.AgentHelper.CleanupAsync(client, agent.Id, thread.Id);
            Console.WriteLine("\n[Cleanup complete]");
        }
    }

    private static async Task<ThreadRun> PollRunAsync(PersistentAgentsClient client, string threadId, string runId)
    {
        ThreadRun run;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  Analyzing");
        do
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            run = await client.Runs.GetRunAsync(threadId, runId);
            Console.Write(".");
        }
        while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);
        Console.ResetColor();
        Console.WriteLine();
        return run;
    }

    private static async Task DisplayLatestResponseAsync(PersistentAgentsClient client, string threadId, ThreadRun run)
    {
        if (run.Status == RunStatus.Failed)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Run failed]: {run.LastError?.Message}");
            Console.ResetColor();
            return;
        }

        await foreach (PersistentThreadMessage msg in client.Messages.GetMessagesAsync(
            threadId: threadId, order: ListSortOrder.Descending))
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
}

using Azure.AI.Agents.Persistent;

namespace AgentFrameworkApp.UseCases;

/// <summary>
/// Use Case 10: Streaming Response Agent
/// Demonstrates streaming agent responses token-by-token for real-time UX.
/// Uses CreateRunStreamingAsync to process streaming updates as they arrive.
/// </summary>
public static class StreamingResponseDemo
{
    public static async Task RunAsync(PersistentAgentsClient client, string modelName)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  USE CASE 10: Streaming Response Agent                  ║");
        Console.WriteLine("║  Responses stream token-by-token in real-time.          ║");
        Console.WriteLine("║  Type 'exit' or 'quit' to end.                          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

        PersistentAgent agent = await client.Administration.CreateAgentAsync(
            model: modelName,
            name: "StreamingStoryteller",
            instructions: """
                You are StreamingStoryteller, a creative and engaging assistant.
                You excel at detailed explanations, storytelling, and technical deep-dives.
                Provide thorough, well-structured responses. Use markdown formatting
                (headers, bullet points, code blocks) when appropriate.
                Aim for rich, detailed answers that showcase the streaming experience.
                """
        );

        Console.WriteLine($"Agent created: {agent.Name} (ID: {agent.Id})");
        Console.WriteLine("─────────────────────────────────────────────────────────\n");

        PersistentAgentThread thread = await client.Threads.CreateThreadAsync();

        try
        {
            // Pre-defined questions that produce rich streaming output
            string[] questions =
            [
                "Explain the evolution of cloud computing — from mainframes to serverless — in a storytelling style.",
                "Write a detailed comparison of REST vs GraphQL vs gRPC with code examples for each.",
                "Describe how a neural network learns, step by step, using a cooking analogy."
            ];

            foreach (string question in questions)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n[You]: {question}");
                Console.ResetColor();

                await client.Messages.CreateMessageAsync(thread.Id, MessageRole.User, question);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\n[Agent]: ");

                // Stream the response
                await foreach (StreamingUpdate update in client.Runs.CreateRunStreamingAsync(thread.Id, agent.Id))
                {
                    if (update is MessageContentUpdate contentUpdate)
                    {
                        Console.Write(contentUpdate.Text);
                    }
                    else if (update.UpdateKind == StreamingUpdateReason.RunFailed)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("\n[Stream error — run failed]");
                        Console.ResetColor();
                        break;
                    }
                }

                Console.ResetColor();
                Console.WriteLine("\n");

                // Small pause between questions for readability
                if (question != questions[^1])
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("  ─── Press Enter for next question ───");
                    Console.ResetColor();
                    Console.ReadLine();
                }
            }
        }
        finally
        {
            await Services.AgentHelper.CleanupAsync(client, agent.Id, thread.Id);
            Console.WriteLine("\n[Cleanup complete]");
        }
    }
}

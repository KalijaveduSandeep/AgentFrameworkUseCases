using System.Text.Json;
using Azure.AI.Agents.Persistent;

namespace AgentFrameworkApp.Services;

/// <summary>
/// Helper methods shared across use-case demos for interacting with agents.
/// </summary>
public static class AgentHelper
{
    /// <summary>
    /// Runs a conversation turn: creates a message, starts a run, polls until complete,
    /// and prints all assistant responses.
    /// </summary>
    public static async Task<PersistentAgentThread> RunConversationTurnAsync(
        PersistentAgentsClient client,
        PersistentAgent agent,
        PersistentAgentThread? thread,
        string userMessage)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[You]: {userMessage}");
        Console.ResetColor();

        // Create thread if it doesn't exist
        if (thread is null)
        {
            thread = await client.Threads.CreateThreadAsync();
        }

        // Add user message
        await client.Messages.CreateMessageAsync(thread.Id, MessageRole.User, userMessage);

        // Create and poll run
        ThreadRun run = await client.Runs.CreateRunAsync(thread, agent);

        do
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            run = await client.Runs.GetRunAsync(thread.Id, run.Id);

            if (run.Status == RunStatus.RequiresAction
                && run.RequiredAction is SubmitToolOutputsAction submitAction)
            {
                var toolOutputs = new List<ToolOutput>();

                foreach (RequiredToolCall toolCall in submitAction.ToolCalls)
                {
                    if (toolCall is RequiredFunctionToolCall funcCall)
                    {
                        string output = FunctionDispatcher.Dispatch(
                            funcCall.Name,
                            funcCall.Arguments);

                        toolOutputs.Add(new ToolOutput(toolCall, output));
                    }
                }

                run = await client.Runs.SubmitToolOutputsToRunAsync(run, toolOutputs, toolApprovals: null);
            }
        }
        while (run.Status == RunStatus.Queued
            || run.Status == RunStatus.InProgress
            || run.Status == RunStatus.RequiresAction);

        if (run.Status == RunStatus.Failed)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Run failed]: {run.LastError?.Message}");
            Console.ResetColor();
            return thread;
        }

        // Retrieve and display the latest assistant messages
        await foreach (PersistentThreadMessage msg in client.Messages.GetMessagesAsync(
            threadId: thread.Id, order: ListSortOrder.Descending))
        {
            if (msg.Role == MessageRole.Agent)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                foreach (MessageContent content in msg.ContentItems)
                {
                    if (content is MessageTextContent textContent)
                    {
                        Console.WriteLine($"\n[Agent]: {textContent.Text}");
                    }
                }
                Console.ResetColor();
                break; // Only show the latest agent reply
            }
        }

        return thread;
    }

    /// <summary>
    /// Cleans up agent and thread resources.
    /// </summary>
    public static async Task CleanupAsync(PersistentAgentsClient client, string agentId, string? threadId)
    {
        try
        {
            if (threadId is not null)
                await client.Threads.DeleteThreadAsync(threadId);

            await client.Administration.DeleteAgentAsync(agentId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Cleanup warning]: {ex.Message}");
        }
    }
}

using Azure.AI.Agents.Persistent;

namespace AgentFrameworkApp.UseCases;

/// <summary>
/// Use Case 7: Real RAG Chat with Azure AI Search
/// Connects to a live Azure AI Search index and enables the agent to retrieve
/// real documents before answering — a production-style Retrieval-Augmented Generation pattern.
/// Supports multi-turn interactive chat so users can ask follow-up questions.
/// </summary>
public static class AzureAISearchRAGDemo
{
    public static async Task RunAsync(PersistentAgentsClient client, string modelName, string connectionId, string indexName)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  USE CASE 7: Real RAG Chat with Azure AI Search         ║");
        Console.WriteLine("║  Agent retrieves documents from your search index.       ║");
        Console.WriteLine("║  Type 'exit' or 'quit' to end the chat.                 ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

        // Configure the Azure AI Search resource pointing to the live index
        var searchToolResource = new AzureAISearchToolResource(
            indexConnectionId: connectionId,
            indexName: indexName,
            topK: 5,
            filter: "",
            queryType: AzureAISearchQueryType.Semantic);

        var toolResources = new ToolResources
        {
            AzureAISearch = searchToolResource
        };

        // Create agent with Azure AI Search tool
        PersistentAgent agent = await client.Administration.CreateAgentAsync(
            model: modelName,
            name: "RAG-SearchAgent",
            instructions: """
                You are a knowledgeable assistant with access to a document search index.
                When the user asks a question, use the Azure AI Search tool to find relevant 
                documents and base your answer on the retrieved content.

                RULES:
                1. Always ground your answers in the search results — do not make up information.
                2. If the search returns relevant documents, summarize the key findings clearly.
                3. If citations are available, reference the source document titles.
                4. If no relevant results are found, tell the user honestly and suggest 
                   rephrasing the question.
                5. Keep answers concise and well-structured using bullet points when appropriate.
                6. You may ask clarifying questions if the user's query is ambiguous.
                """,
            tools: [new AzureAISearchToolDefinition()],
            toolResources: toolResources);

        Console.WriteLine($"Agent created: {agent.Name} (ID: {agent.Id})");
        Console.WriteLine($"Connected to index: {indexName}");
        Console.WriteLine("─────────────────────────────────────────────────────────\n");

        PersistentAgentThread? thread = null;
        try
        {
            // Create a thread for the conversation
            thread = await client.Threads.CreateThreadAsync();

            // Interactive chat loop
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("[You]: ");
                Console.ResetColor();

                string? userInput = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(userInput))
                    continue;

                if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase)
                    || userInput.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("\nEnding RAG chat session.");
                    break;
                }

                // Add user message
                await client.Messages.CreateMessageAsync(thread.Id, MessageRole.User, userInput);

                // Create run and poll
                ThreadRun run = await client.Runs.CreateRunAsync(thread, agent);

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("  Searching & generating");
                Console.ResetColor();

                do
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    run = await client.Runs.GetRunAsync(thread.Id, run.Id);

                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(".");
                    Console.ResetColor();
                }
                while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);

                Console.WriteLine(); // end the dots line

                if (run.Status == RunStatus.Failed)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Run failed]: {run.LastError?.Message}");
                    Console.ResetColor();
                    continue;
                }

                // Retrieve and display the latest agent response with citations
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
                                // Process citations if present
                                if (textContent.Annotations.Count > 0)
                                {
                                    string annotatedText = textContent.Text;
                                    foreach (MessageTextAnnotation annotation in textContent.Annotations)
                                    {
                                        if (annotation is MessageTextUriCitationAnnotation uriAnnotation)
                                        {
                                            annotatedText = annotatedText.Replace(
                                                uriAnnotation.Text,
                                                $" [{uriAnnotation.UriCitation.Title}]({uriAnnotation.UriCitation.Uri})");
                                        }
                                    }
                                    Console.WriteLine($"\n[Agent]: {annotatedText}");
                                }
                                else
                                {
                                    Console.WriteLine($"\n[Agent]: {textContent.Text}");
                                }
                            }
                        }
                        Console.ResetColor();
                        break; // Only show latest agent reply
                    }
                }

                Console.WriteLine();
            }
        }
        finally
        {
            await Services.AgentHelper.CleanupAsync(client, agent.Id, thread?.Id);
            Console.WriteLine("[Cleanup complete]");
        }
    }
}

using System.Text.Json;
using Azure.AI.Agents.Persistent;

namespace AgentFrameworkApp.UseCases;

/// <summary>
/// Use Case 15: Agent with Structured Output
/// Demonstrates forcing the agent to return JSON matching a specific schema.
/// Critical for downstream processing, API responses, and data pipelines.
/// </summary>
public static class StructuredOutputDemo
{
    public static async Task RunAsync(PersistentAgentsClient client, string modelName)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  USE CASE 15: Structured Output Agent                   ║");
        Console.WriteLine("║  Agent returns responses in strict JSON format.          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

        // ─── Example 1: Product Review Analyzer ──────────────────────────────
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("── Example 1: Product Review Sentiment Analysis ─────────");
        Console.ResetColor();

        PersistentAgent reviewAgent = await client.Administration.CreateAgentAsync(
            model: modelName,
            name: "ReviewAnalyzer",
            instructions: """
                You analyze product reviews and return ONLY a valid JSON object.
                No markdown, no explanation — just the raw JSON.

                Required JSON schema:
                {
                  "sentiment": "positive" | "negative" | "mixed" | "neutral",
                  "confidence": 0.0 to 1.0,
                  "keyTopics": ["topic1", "topic2"],
                  "pros": ["pro1", "pro2"],
                  "cons": ["con1", "con2"],
                  "suggestedRating": 1 to 5,
                  "summary": "One-sentence summary"
                }

                Always return valid, parseable JSON.
                """,
            responseFormat: BinaryData.FromObjectAsJson(new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "review_analysis",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            sentiment = new { type = "string", @enum = new[] { "positive", "negative", "mixed", "neutral" } },
                            confidence = new { type = "number" },
                            keyTopics = new { type = "array", items = new { type = "string" } },
                            pros = new { type = "array", items = new { type = "string" } },
                            cons = new { type = "array", items = new { type = "string" } },
                            suggestedRating = new { type = "number" },
                            summary = new { type = "string" }
                        },
                        required = new[] { "sentiment", "confidence", "keyTopics", "pros", "cons", "suggestedRating", "summary" },
                        additionalProperties = false
                    }
                }
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        );

        Console.WriteLine($"Agent created: {reviewAgent.Name}");

        string[] reviews =
        [
            """
            I've been using the SmartWidget Pro for 3 months in our factory. The sensor 
            accuracy is outstanding — temperature readings are within 0.1°C of our reference 
            instrument. Battery life is phenomenal, still at 92% after 3 months. However, 
            the initial Wi-Fi setup was painful — took our IT team 2 hours to configure. 
            The mobile app is clunky and crashes on Android. Would love OTA updates to fix that.
            Overall great hardware, needs software polish. 4/5 stars.
            """,
            """
            Terrible experience. Ordered 50 units for our warehouse. 8 arrived DOA. The 
            ones that work lose Bluetooth connection every few hours. Customer support took 
            3 days to respond and then just sent generic troubleshooting steps. For $299/unit 
            this is unacceptable. Returning all of them and going with CompetitorX.
            """
        ];

        PersistentAgentThread? thread1 = null;
        try
        {
            foreach (string review in reviews)
            {
                thread1 = await RunAndDisplayStructuredAsync(
                    client, reviewAgent, thread1, $"Analyze this review:\n{review}");
            }
        }
        finally
        {
            await Services.AgentHelper.CleanupAsync(client, reviewAgent.Id, thread1?.Id);
        }

        // ─── Example 2: Data Extractor ───────────────────────────────────────
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n── Example 2: Contact Information Extractor ────────────");
        Console.ResetColor();

        PersistentAgent extractorAgent = await client.Administration.CreateAgentAsync(
            model: modelName,
            name: "DataExtractor",
            instructions: """
                You extract structured contact information from unstructured text.
                Return ONLY valid JSON. No markdown, no explanation.
                
                Required JSON schema:
                {
                  "contacts": [
                    {
                      "name": "Full Name",
                      "email": "email@domain.com or null",
                      "phone": "phone number or null",
                      "company": "company name or null",
                      "role": "job title or null"
                    }
                  ],
                  "totalFound": number
                }
                """,
            responseFormat: BinaryData.FromObjectAsJson(new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "contact_extraction",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            contacts = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        name = new { type = "string" },
                                        email = new { type = new[] { "string", "null" } },
                                        phone = new { type = new[] { "string", "null" } },
                                        company = new { type = new[] { "string", "null" } },
                                        role = new { type = new[] { "string", "null" } }
                                    },
                                    required = new[] { "name", "email", "phone", "company", "role" },
                                    additionalProperties = false
                                }
                            },
                            totalFound = new { type = "number" }
                        },
                        required = new[] { "contacts", "totalFound" },
                        additionalProperties = false
                    }
                }
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        );

        Console.WriteLine($"Agent created: {extractorAgent.Name}");

        PersistentAgentThread? thread2 = null;
        try
        {
            thread2 = await RunAndDisplayStructuredAsync(
                client, extractorAgent, thread2,
                """
                Extract contacts from this email thread:

                Hey team, please reach out to the following people for the Q1 review:
                - Sarah Johnson, VP of Engineering at CloudScale Inc. (sarah.j@cloudscale.io, 555-0142)
                - Dr. Michael Chen from the AI Research Lab (m.chen@airl.org)
                - Lisa Park, our new Sales Director — her number is (415) 555-0198 but 
                  I don't have her email yet.
                - Also CC Robert Williams at robert@techpartner.com — he's the CTO there.
                """);
        }
        finally
        {
            await Services.AgentHelper.CleanupAsync(client, extractorAgent.Id, thread2?.Id);
        }

        // ─── Example 3: Code Review Reporter ─────────────────────────────────
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n── Example 3: Code Review Analyzer ─────────────────────");
        Console.ResetColor();

        PersistentAgent codeReviewAgent = await client.Administration.CreateAgentAsync(
            model: modelName,
            name: "CodeReviewer",
            instructions: """
                You review code snippets and return ONLY a valid JSON object.
                No markdown, no explanation.
                
                Required JSON schema:
                {
                  "overallScore": 1-10,
                  "issues": [
                    {
                      "severity": "critical" | "warning" | "info",
                      "line": "approximate line reference",
                      "description": "what's wrong",
                      "suggestion": "how to fix it"
                    }
                  ],
                  "positives": ["good practices found"],
                  "refactoringSuggestions": ["high-level improvement suggestions"]
                }
                """,
            responseFormat: BinaryData.FromObjectAsJson(new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "code_review",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            overallScore = new { type = "number" },
                            issues = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        severity = new { type = "string", @enum = new[] { "critical", "warning", "info" } },
                                        line = new { type = "string" },
                                        description = new { type = "string" },
                                        suggestion = new { type = "string" }
                                    },
                                    required = new[] { "severity", "line", "description", "suggestion" },
                                    additionalProperties = false
                                }
                            },
                            positives = new { type = "array", items = new { type = "string" } },
                            refactoringSuggestions = new { type = "array", items = new { type = "string" } }
                        },
                        required = new[] { "overallScore", "issues", "positives", "refactoringSuggestions" },
                        additionalProperties = false
                    }
                }
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        );

        Console.WriteLine($"Agent created: {codeReviewAgent.Name}");

        PersistentAgentThread? thread3 = null;
        try
        {
            thread3 = await RunAndDisplayStructuredAsync(
                client, codeReviewAgent, thread3,
                """
                Review this C# code:

                public class UserService
                {
                    string connectionString = "Server=prod-db;Password=admin123;";
                    
                    public User GetUser(string id)
                    {
                        var conn = new SqlConnection(connectionString);
                        conn.Open();
                        var cmd = new SqlCommand("SELECT * FROM Users WHERE Id = '" + id + "'", conn);
                        var reader = cmd.ExecuteReader();
                        reader.Read();
                        return new User { Name = reader["Name"].ToString(), Email = reader["Email"].ToString() };
                    }
                    
                    public void DeleteAllUsers()
                    {
                        // TODO: add confirmation
                        var conn = new SqlConnection(connectionString);
                        conn.Open();
                        new SqlCommand("DELETE FROM Users", conn).ExecuteNonQuery();
                    }
                }
                """);
        }
        finally
        {
            await Services.AgentHelper.CleanupAsync(client, codeReviewAgent.Id, thread3?.Id);
            Console.WriteLine("\n[All cleanup complete]");
        }
    }

    private static async Task<PersistentAgentThread?> RunAndDisplayStructuredAsync(
        PersistentAgentsClient client, PersistentAgent agent,
        PersistentAgentThread? thread, string userMessage)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[You]: {(userMessage.Length > 120 ? userMessage[..120] + "..." : userMessage)}");
        Console.ResetColor();

        if (thread is null)
            thread = await client.Threads.CreateThreadAsync();

        await client.Messages.CreateMessageAsync(thread.Id, MessageRole.User, userMessage);
        ThreadRun run = await client.Runs.CreateRunAsync(thread, agent);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  Processing");
        do
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            run = await client.Runs.GetRunAsync(thread.Id, run.Id);
            Console.Write(".");
        }
        while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);
        Console.ResetColor();
        Console.WriteLine();

        if (run.Status == RunStatus.Failed)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Run failed]: {run.LastError?.Message}");
            Console.ResetColor();
            return thread;
        }

        await foreach (PersistentThreadMessage msg in client.Messages.GetMessagesAsync(
            threadId: thread.Id, order: ListSortOrder.Descending))
        {
            if (msg.Role == MessageRole.Agent)
            {
                foreach (MessageContent content in msg.ContentItems)
                {
                    if (content is MessageTextContent textContent)
                    {
                        // Try to pretty-print JSON
                        try
                        {
                            var jsonDoc = JsonDocument.Parse(textContent.Text);
                            string pretty = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true });
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"\n[Agent — Structured JSON]:\n{pretty}");
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"\n[Agent]: {textContent.Text}");
                        }
                        Console.ResetColor();
                    }
                }
                break;
            }
        }

        return thread;
    }
}

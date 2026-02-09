using System.Text;
using Azure.AI.Agents.Persistent;

namespace AgentFrameworkApp.UseCases;

/// <summary>
/// Use Case 9: File Search Agent
/// Demonstrates the built-in File Search tool which lets the agent search
/// through uploaded documents using the native vector store.
/// Files are uploaded, added to a vector store, and the agent queries them.
/// </summary>
public static class FileSearchDemo
{
    public static async Task RunAsync(PersistentAgentsClient client, string modelName)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  USE CASE 9: File Search Agent                          ║");
        Console.WriteLine("║  Agent searches uploaded documents via native vector     ║");
        Console.WriteLine("║  store (built-in File Search tool).                      ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

        // ─── Step 1: Create sample documents in memory ───────────────────────
        Console.WriteLine("Uploading sample documents...");

        // Upload sample files
        var file1 = await client.Files.UploadFileAsync(
            data: new MemoryStream(Encoding.UTF8.GetBytes("""
                # Product Specification: SmartWidget Pro

                ## Overview
                SmartWidget Pro is our flagship IoT device designed for industrial monitoring.
                Release Date: March 2025. Price: $299/unit (volume discounts available).

                ## Technical Specifications
                - Processor: ARM Cortex-M7, 480 MHz
                - Memory: 512 KB RAM, 2 MB Flash
                - Connectivity: Wi-Fi 6, Bluetooth 5.2, LoRaWAN
                - Sensors: Temperature (-40°C to 125°C), Humidity, Pressure, Vibration
                - Battery Life: Up to 5 years on CR2477 coin cell
                - Enclosure: IP67 rated, industrial-grade aluminum

                ## Key Features
                1. Real-time anomaly detection using edge ML models
                2. OTA firmware updates via secure boot
                3. Dashboard integration with Azure IoT Hub and AWS IoT Core
                4. REST API for custom integrations
                5. Multi-language SDK support (Python, C#, JavaScript)

                ## Compliance
                - CE, FCC, and UL certified
                - GDPR and SOC 2 Type II compliant
                - ISO 27001 security certified
                """)),
            purpose: PersistentAgentFilePurpose.Agents,
            filename: "smartwidget_pro_spec.md");

        var file2 = await client.Files.UploadFileAsync(
            data: new MemoryStream(Encoding.UTF8.GetBytes("""
                # Company HR Policy Document

                ## Remote Work Policy (Effective January 2025)
                - All employees are eligible for hybrid work (3 days office, 2 days remote).
                - Fully remote positions require VP approval.
                - Home office stipend: $500/year for equipment.
                - Core hours: 10 AM–3 PM local time for meetings.

                ## Leave Policy
                - Annual PTO: 20 days for all employees, 25 days after 5 years.
                - Sick leave: 10 days/year (no rollover).
                - Parental leave: 16 weeks paid for primary caregiver, 8 weeks for secondary.
                - Bereavement leave: 5 days for immediate family.

                ## Benefits
                - Health insurance: Comprehensive plan (medical, dental, vision) — 90% company paid.
                - 401(k) match: 100% up to 6% of salary.
                - Learning & Development budget: $2,000/year per employee.
                - Gym membership reimbursement: up to $75/month.

                ## Performance Reviews
                - Bi-annual reviews (June and December).
                - Rating scale: 1 (Below Expectations) to 5 (Exceptional).
                - Promotion eligibility requires rating of 4+ for two consecutive cycles.
                """)),
            purpose: PersistentAgentFilePurpose.Agents,
            filename: "hr_policy.md");

        var file3 = await client.Files.UploadFileAsync(
            data: new MemoryStream(Encoding.UTF8.GetBytes("""
                # Q4 2025 Sales Report

                ## Executive Summary
                Total revenue for Q4 2025 was $12.4M, representing a 23% YoY increase.
                The EMEA region showed the strongest growth at 31%, while APAC grew 18%.

                ## Revenue Breakdown by Region
                | Region | Q4 2025 | Q4 2024 | Growth |
                |--------|---------|---------|--------|
                | North America | $5.2M | $4.5M | 15.6% |
                | EMEA | $4.1M | $3.1M | 32.3% |
                | APAC | $2.3M | $1.9M | 21.1% |
                | LATAM | $0.8M | $0.6M | 33.3% |

                ## Top Products
                1. SmartWidget Pro — $4.8M (39% of total)
                2. DataSync Platform — $3.6M (29% of total)
                3. CloudMonitor Suite — $2.1M (17% of total)
                4. Professional Services — $1.9M (15% of total)

                ## Key Metrics
                - New customers acquired: 142
                - Customer retention rate: 94.2%
                - Average deal size: $87,300 (up from $72,100)
                - Sales cycle length: 45 days average (down from 58 days)

                ## Forecast
                Q1 2026 pipeline: $15.8M with 68% weighted probability.
                Expected close: $10.7M (projected 14% QoQ growth).
                """)),
            purpose: PersistentAgentFilePurpose.Agents,
            filename: "q4_2025_sales_report.md");

        Console.WriteLine($"  Uploaded: smartwidget_pro_spec.md ({file1.Value.Id})");
        Console.WriteLine($"  Uploaded: hr_policy.md ({file2.Value.Id})");
        Console.WriteLine($"  Uploaded: q4_2025_sales_report.md ({file3.Value.Id})");

        // ─── Step 2: Create vector store and add files ───────────────────────
        Console.WriteLine("\nCreating vector store and indexing documents...");

        PersistentAgentsVectorStore vectorStore = await client.VectorStores.CreateVectorStoreAsync(
            fileIds: [file1.Value.Id, file2.Value.Id, file3.Value.Id],
            name: "DemoDocumentStore");

        Console.WriteLine($"  Vector store created: {vectorStore.Name} (ID: {vectorStore.Id})");

        // Wait for indexing to complete
        // Allow time for indexing
        await Task.Delay(TimeSpan.FromSeconds(3));
        vectorStore = await client.VectorStores.GetVectorStoreAsync(vectorStore.Id);

        Console.WriteLine($"  Indexing status: {vectorStore.Status} ({vectorStore.FileCounts.Completed} files indexed)");

        // ─── Step 3: Create agent with file search tool ──────────────────────
        var toolResources = new ToolResources
        {
            FileSearch = new FileSearchToolResource
            {
                VectorStoreIds = { vectorStore.Id }
            }
        };

        PersistentAgent agent = await client.Administration.CreateAgentAsync(
            model: modelName,
            name: "DocSearchAgent",
            instructions: """
                You are DocSearchAgent, a precise document search assistant.
                You have access to uploaded company documents via file search.

                RULES:
                1. Always use the file search tool to find relevant information.
                2. Quote specific data points (numbers, dates, percentages) from the documents.
                3. If the information is not found in any document, say so clearly.
                4. Reference which document the information came from.
                5. Present findings in a structured, easy-to-read format.
                """,
            tools: [new FileSearchToolDefinition()],
            toolResources: toolResources);

        Console.WriteLine($"\nAgent created: {agent.Name} (ID: {agent.Id})");
        Console.WriteLine("─────────────────────────────────────────────────────────\n");

        PersistentAgentThread? thread = null;
        try
        {
            // Turn 1: Product query
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "What are the technical specifications of the SmartWidget Pro? What sensors does it include?");

            // Turn 2: HR policy query
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "How much PTO do I get? And what's the parental leave policy?");

            // Turn 3: Sales data query
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "What was the total Q4 2025 revenue and which region grew the fastest?");

            // Turn 4: Cross-document query
            thread = await Services.AgentHelper.RunConversationTurnAsync(
                client, agent, thread,
                "How much revenue did SmartWidget Pro generate, and what is its price per unit?");
        }
        finally
        {
            // Cleanup: agent, thread, vector store, and files
            await Services.AgentHelper.CleanupAsync(client, agent.Id, thread?.Id);

            try
            {
                await client.VectorStores.DeleteVectorStoreAsync(vectorStore.Id);
                await client.Files.DeleteFileAsync(file1.Value.Id);
                await client.Files.DeleteFileAsync(file2.Value.Id);
                await client.Files.DeleteFileAsync(file3.Value.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[File cleanup warning]: {ex.Message}");
            }

            Console.WriteLine("\n[Cleanup complete]");
        }
    }
}

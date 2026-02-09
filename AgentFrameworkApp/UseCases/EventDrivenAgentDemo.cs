using System.Text.Json;
using Azure.AI.Agents.Persistent;

namespace AgentFrameworkApp.UseCases;

/// <summary>
/// Use Case 14: Async Event-Driven Agent
/// Simulates an event-driven pipeline where incoming events (new email, alert, 
/// ticket) are processed by the agent, which decides on an action and generates
/// a structured response. Demonstrates real-world integration patterns beyond Q&A.
/// </summary>
public static class EventDrivenAgentDemo
{
    public static async Task RunAsync(PersistentAgentsClient client, string modelName)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  USE CASE 14: Event-Driven Agent                        ║");
        Console.WriteLine("║  Agent processes simulated events (emails, alerts,       ║");
        Console.WriteLine("║  tickets) and decides on automated actions.              ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

        PersistentAgent agent = await client.Administration.CreateAgentAsync(
            model: modelName,
            name: "EventProcessor",
            instructions: """
                You are EventProcessor, an intelligent event-processing agent.
                
                You receive events from various systems (emails, monitoring alerts, 
                support tickets). For each event you must:

                1. **Classify** the event: determine severity (Critical/High/Medium/Low) 
                   and category (Bug, Feature Request, Outage, Customer Inquiry, etc.)
                2. **Summarize** the event in 1-2 sentences.
                3. **Decide on an action**: 
                   - Route to a team (Engineering, Support, Sales, Management)
                   - Suggest an automated response
                   - Flag for human review if ambiguous
                4. **Output a structured JSON response** with these fields:
                   {
                     "eventId": "...",
                     "severity": "Critical|High|Medium|Low",
                     "category": "...",
                     "summary": "...",
                     "action": "...",
                     "routeTo": "...",
                     "autoResponse": "..." (suggested reply if applicable)
                   }

                After the JSON, briefly explain your reasoning in plain text.
                """
        );

        Console.WriteLine($"Agent created: {agent.Name} (ID: {agent.Id})");
        Console.WriteLine("─────────────────────────────────────────────────────────\n");

        // Simulated events from various systems
        var events = new[]
        {
            new SimulatedEvent(
                "EVT-001",
                "Email",
                """
                From: angry.customer@bigcorp.com
                Subject: URGENT — Our production system is down!
                Body: Hi, our entire production environment has been down for 2 hours. 
                We're losing $50K/hour. We need immediate help. This started after 
                your latest patch (v3.2.1) was applied. Please escalate IMMEDIATELY.
                Our account ID is CORP-9912.
                """),

            new SimulatedEvent(
                "EVT-002",
                "Monitoring Alert",
                """
                Alert: CPU_THRESHOLD_EXCEEDED
                Service: api-gateway-prod
                Region: East US
                Current CPU: 94.2% (threshold: 80%)
                Duration: 15 minutes
                Correlated alerts: Memory at 78%, Response latency P99 = 4200ms (normal: 200ms)
                Last deployment: 45 minutes ago (deploy-id: d-8834)
                """),

            new SimulatedEvent(
                "EVT-003",
                "Support Ticket",
                """
                Ticket #TK-4421
                Customer: Jane Smith (jane@startup.io, Pro Plan)
                Subject: How to set up SSO with Okta?
                Description: We recently upgraded to the Pro plan and want to configure 
                SSO using Okta as our identity provider. I followed the docs but got stuck 
                at the SAML configuration step. Can someone walk me through it or provide 
                a video tutorial? Not urgent but would like help this week.
                """),

            new SimulatedEvent(
                "EVT-004",
                "Email",
                """
                From: cto@techpartner.com
                Subject: Partnership opportunity — AI integration
                Body: Hi team, we're impressed with your AI agent platform and would like 
                to explore a partnership. We have 500+ enterprise customers who could benefit 
                from integrating your agents into our workflow platform. Would love to schedule 
                a call next week to discuss API access, pricing tiers, and co-marketing. 
                Looking forward to hearing from you.
                """),

            new SimulatedEvent(
                "EVT-005",
                "Monitoring Alert",
                """
                Alert: SECURITY_ANOMALY_DETECTED
                Service: auth-service-prod
                Details: 847 failed login attempts from IP range 103.45.xx.xx in the last 
                10 minutes. Pattern consistent with credential stuffing attack. 
                Rate limiting activated. No successful breaches detected yet.
                Affected accounts: 12 accounts locked due to failed attempt threshold.
                """)
        };

        PersistentAgentThread? thread = null;
        try
        {
            foreach (var evt in events)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"── Processing Event: {evt.Id} ({evt.Source}) ──────────");
                Console.ResetColor();

                string eventPayload = $"""
                    [INCOMING EVENT]
                    Event ID: {evt.Id}
                    Source: {evt.Source}
                    Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss} UTC
                    
                    Payload:
                    {evt.Payload}
                    
                    Please classify, summarize, and decide on an action for this event.
                    """;

                // Each event gets its own thread for isolation
                thread = await Services.AgentHelper.RunConversationTurnAsync(
                    client, agent, thread: null, eventPayload);

                // Clean up the thread (but keep the agent for next event)
                if (thread is not null)
                {
                    try { await client.Threads.DeleteThreadAsync(thread.Id); }
                    catch { /* ignore */ }
                    thread = null;
                }

                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("── ALL EVENTS PROCESSED ──────────────────────────────");
            Console.WriteLine($"  Total events: {events.Length}");
            Console.ResetColor();
        }
        finally
        {
            await client.Administration.DeleteAgentAsync(agent.Id);
            if (thread is not null)
            {
                try { await client.Threads.DeleteThreadAsync(thread.Id); }
                catch { /* ignore */ }
            }
            Console.WriteLine("\n[Cleanup complete]");
        }
    }

    private record SimulatedEvent(string Id, string Source, string Payload);
}

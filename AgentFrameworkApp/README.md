# Azure AI Foundry – Agent Framework Demo (.NET)

A .NET 8 console application demonstrating **5 different use cases** of the Azure AI Agent Service using the `Azure.AI.Agents.Persistent` SDK, deployed through **Microsoft Azure AI Foundry**.

## Use Cases

| # | Demo | Description |
|---|------|-------------|
| 1 | **Basic Conversational Agent** | A "TechAdvisor" persona that answers questions about software architecture, cloud computing, and AI. Demonstrates multi-turn conversation with memory. |
| 2 | **Code Interpreter Agent** | A "DataAnalyst" that writes and executes Python code for Fibonacci sequences, sales data analysis, and palindrome algorithms. |
| 3 | **Function Calling Agent** | A "MarketAssistant" that calls local `get_weather` and `get_stock_price` functions, demonstrating tool integration. |
| 4 | **Knowledge Base Agent (RAG)** | A "SupportAgent" that searches a simulated knowledge base before answering—refund policies, pricing plans, security certs. |
| 5 | **Multi-Tool Agent** | A "UniversalAssistant" combining Code Interpreter + weather + stocks + calculator + knowledge base. The agent picks the right tool(s) per question. |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- An **Azure AI Foundry** project with a deployed model (e.g., `gpt-4o`)
- Azure CLI authenticated (`az login`) or another credential supported by `DefaultAzureCredential`

## Setup

1. **Clone / open** this folder in VS Code.

2. **Configure your endpoint** — edit `appsettings.json`:

   ```json
   {
     "AzureAI": {
       "ConnectionString": "https://<your-project>.services.ai.azure.com",
       "ModelDeploymentName": "gpt-4o"
     }
   }
   ```

   > `ConnectionString` is the **Project Endpoint** from your Azure AI Foundry project overview page.

3. **Restore & build**:

   ```bash
   cd AgentFrameworkApp
   dotnet build
   ```

4. **Run**:

   ```bash
   dotnet run
   ```

   You'll see an interactive menu to pick individual demos or run all five sequentially.

## Project Structure

```
AgentFrameworkApp/
├── Program.cs                          # Entry point with interactive menu
├── appsettings.json                    # Configuration (endpoint + model name)
├── Models/
│   └── WeatherData.cs                  # Data records for function tool responses
├── Services/
│   ├── AgentClientFactory.cs           # Singleton PersistentAgentsClient factory
│   ├── AgentHelper.cs                  # Shared run-conversation & cleanup logic
│   └── FunctionDispatcher.cs           # Routes function tool calls to local handlers
└── UseCases/
    ├── BasicConversationDemo.cs        # Use Case 1: Conversational agent
    ├── CodeInterpreterDemo.cs          # Use Case 2: Code interpreter
    ├── FunctionCallingDemo.cs          # Use Case 3: Weather & stock tools
    ├── KnowledgeBaseDemo.cs            # Use Case 4: RAG pattern
    └── MultiToolDemo.cs               # Use Case 5: All tools combined
```

## Key Packages

| Package | Purpose |
|---------|---------|
| `Azure.AI.Agents.Persistent` | Agent creation, threads, messages, runs, tool definitions |
| `Azure.Identity` | `DefaultAzureCredential` for authentication |
| `Microsoft.Extensions.Configuration` | JSON + environment variable configuration |

## How It Works

1. **`PersistentAgentsClient`** connects to your Azure AI Foundry project endpoint.
2. Each demo creates an **agent** with specific instructions and tools via `client.Administration.CreateAgentAsync(...)`.
3. A **thread** is created, messages are added, and a **run** is started.
4. The helper polls the run status. When the agent calls a function tool (`RunStatus.RequiresAction`), the `FunctionDispatcher` executes it locally and submits the result back.
5. After completion, agent responses are printed and resources are cleaned up.

## Authentication

The app uses `DefaultAzureCredential`, which tries (in order):
- Environment variables
- Managed Identity
- Azure CLI (`az login`)
- Visual Studio / VS Code credentials

Make sure you're signed in with `az login` or have the appropriate environment set up.

## License

MIT

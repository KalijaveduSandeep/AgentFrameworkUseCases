# Azure AI Foundry – Agent Framework Demo (.NET)

A .NET 8 console application demonstrating **15 different use cases** of the Azure AI Agent Service using the `Azure.AI.Agents.Persistent` SDK, deployed through **Microsoft Azure AI Foundry**.

## Use Cases

### Original Demos (1–6)

| # | Demo | Description |
|---|------|-------------|
| 1 | **Basic Conversational Agent** | A "TechAdvisor" persona that answers questions about software architecture, cloud computing, and AI. Demonstrates multi-turn conversation with memory. |
| 2 | **Code Interpreter Agent** | A "DataAnalyst" that writes and executes Python code for Fibonacci sequences, sales data analysis, and palindrome algorithms. |
| 3 | **Function Calling Agent** | A "MarketAssistant" that calls local `get_weather` and `get_stock_price` functions, demonstrating tool integration. |
| 4 | **Knowledge Base Agent (RAG)** | A "SupportAgent" that searches a simulated knowledge base before answering—refund policies, pricing plans, security certs. |
| 5 | **Multi-Tool Agent** | A "UniversalAssistant" combining Code Interpreter + weather + stocks + calculator + knowledge base. The agent picks the right tool(s) per question. |
| 6 | **Azure AI Search RAG Chat** | A "RAG-SearchAgent" that connects to a **live Azure AI Search index** to retrieve real documents before answering. Supports multi-turn interactive chat with citation support. |

### High Priority – Key Architecture Patterns (7–9)

| # | Demo | Description |
|---|------|-------------|
| 7 | **Multi-Agent Orchestration** | Two agents collaborating — a "Researcher" gathers data, then a "Writer" produces a polished article. Demonstrates the **Workflow Orchestration** pattern. |
| 8 | **File Search Agent** | Uploads 3 sample documents (product spec, HR policy, sales report) to a vector store. Agent searches them using the built-in **File Search** tool. |
| 9 | **Streaming Response Agent** | Streams agent responses **token-by-token** in real-time using `CreateRunStreamingAsync`. Essential for production UX. |

### Medium Priority – Practical Patterns (10–13)

| # | Demo | Description |
|---|------|-------------|
| 10 | **Image / Vision Agent** | Multi-modal input — sends images (architecture diagram, code screenshot) and asks the agent to analyze them. Supports custom image URLs. |
| 11 | **Conversation Memory & History** | Persists thread IDs to a local JSON file so users can **resume past conversations**. View history, list saved threads, start new sessions. |
| 12 | **Guardrails & Safety Agent** | Responsible AI demo with 6 test scenarios: prompt injection, system prompt extraction, social engineering, medical advice boundaries, and recovery. |
| 13 | **Event-Driven Agent** | Processes 5 simulated events (customer emails, monitoring alerts, support tickets) — classifies severity, decides actions, and routes to teams with structured JSON output. |

### Simple Priority – Production Patterns (14–15)

| # | Demo | Description |
|---|------|-------------|
| 14 | **Structured Output Agent** | Forces the agent to return **strict JSON** via `json_schema` response format. Three examples: product review analysis, contact extraction, and code review. |
| 15 | **Error Handling & Retry Patterns** | Production-grade resilience: exponential backoff, timeout handling with run cancellation, safe tool execution wrappers, and graceful resource cleanup. |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- An **Azure AI Foundry** project with a deployed model (e.g., `gpt-4o`, `gpt-4o-mini`)
- Azure CLI authenticated (`az login`) or another credential supported by `DefaultAzureCredential`
- *(For Use Case 6)* An **Azure AI Search** resource with an existing index and a connection configured in Azure AI Foundry
- *(For Use Case 10)* A model deployment that supports **vision/multi-modal** input (e.g., `gpt-4o`)

## Setup

1. **Clone / open** this folder in VS Code.

2. **Configure your endpoint** — edit `appsettings.json`:

   ```json
   {
     "AzureAI": {
       "ConnectionString": "https://<your-project>.services.ai.azure.com/api/projects/<project-name>",
       "ModelDeploymentName": "gpt-4o"
     },
     "AzureAISearch": {
       "ConnectionId": "<your-azure-ai-search-connection-resource-id>",
       "IndexName": "<your-search-index-name>"
     }
   }
   ```

   > **`AzureAI:ConnectionString`** — the Project Endpoint from your Azure AI Foundry project overview page.
   >
   > **`AzureAISearch:ConnectionId`** — the full resource ID of the Azure AI Search connection in your AI Foundry project. Found under **Project settings → Connected resources** in Azure AI Foundry.
   >
   > **`AzureAISearch:IndexName`** — the name of your Azure AI Search index (e.g., `azureblob-index`).

3. **Restore & build**:

   ```bash
   cd AgentFrameworkApp
   dotnet build
   ```

4. **Run**:

   ```bash
   dotnet run
   ```

   You'll see a categorized interactive menu to pick individual demos or run all sequentially (option 99).

## Project Structure

```
AgentFrameworkApp/
├── Program.cs                              # Entry point with categorized interactive menu
├── appsettings.json                        # Configuration (endpoint, model, search settings)
├── Models/
│   └── WeatherData.cs                      # Data records for function tool responses
├── Services/
│   ├── AgentClientFactory.cs               # Singleton PersistentAgentsClient factory
│   ├── AgentHelper.cs                      # Shared run-conversation & cleanup logic
│   └── FunctionDispatcher.cs               # Routes function tool calls to local handlers
└── UseCases/
    ├── BasicConversationDemo.cs            # Use Case 1:  Conversational agent
    ├── CodeInterpreterDemo.cs              # Use Case 2:  Code interpreter
    ├── FunctionCallingDemo.cs              # Use Case 3:  Weather & stock tools
    ├── KnowledgeBaseDemo.cs                # Use Case 4:  RAG pattern (simulated)
    ├── MultiToolDemo.cs                    # Use Case 5:  All tools combined
    ├── AzureAISearchRAGDemo.cs             # Use Case 6:  Live Azure AI Search RAG
    ├── MultiAgentOrchestrationDemo.cs      # Use Case 7:  Researcher → Writer pipeline
    ├── FileSearchDemo.cs                   # Use Case 8:  Document upload & vector search
    ├── StreamingResponseDemo.cs            # Use Case 9:  Token-by-token streaming
    ├── ImageVisionDemo.cs                  # Use Case 10: Multi-modal image analysis
    ├── ConversationMemoryDemo.cs           # Use Case 11: Persistent conversation history
    ├── GuardrailsSafetyDemo.cs             # Use Case 12: Responsible AI guardrails
    ├── EventDrivenAgentDemo.cs             # Use Case 13: Event processing pipeline
    ├── StructuredOutputDemo.cs             # Use Case 14: Strict JSON schema output
    └── ErrorHandlingRetryDemo.cs           # Use Case 15: Retry, timeout & resilience
```

## Key Packages

| Package | Purpose |
|---------|---------|
| `Azure.AI.Agents.Persistent` | Agent creation, threads, messages, runs, tool definitions, streaming, file search |
| `Azure.Identity` | `DefaultAzureCredential` for authentication |
| `Microsoft.Extensions.Configuration` | JSON + environment variable configuration |

## How It Works

1. **`PersistentAgentsClient`** connects to your Azure AI Foundry project endpoint.
2. Each demo creates an **agent** with specific instructions and tools via `client.Administration.CreateAgentAsync(...)`.
3. A **thread** is created, messages are added, and a **run** is started.
4. The helper polls the run status. When the agent calls a function tool (`RunStatus.RequiresAction`), the `FunctionDispatcher` executes it locally and submits the result back.
5. After completion, agent responses are printed and resources are cleaned up.

## Core Components Coverage

| Component | Use Cases | Status |
|-----------|-----------|--------|
| **Chat Clients** | All (1–15) | ✅ Unified `PersistentAgentsClient` |
| **Function Tools** | 3, 4, 5, 13, 15 | ✅ Custom functions via `FunctionToolDefinition` |
| **Built-in Tools** | 2 (Code Interpreter), 6 (AI Search), 8 (File Search) | ✅ All three built-in tools |
| **Conversation Management** | 1, 6, 11 | ✅ Multi-turn threads + persistent history |
| **Workflow Orchestration** | 7 | ✅ Researcher → Writer agent pipeline |
| **Streaming** | 9 | ✅ Real-time token streaming |
| **Multi-Modal** | 10 | ✅ Image + text input |
| **Structured Output** | 14 | ✅ JSON schema enforcement |
| **Guardrails / Safety** | 12 | ✅ Prompt injection protection |
| **Error Handling** | 15 | ✅ Retry, timeout, graceful degradation |

### Use Case 6 – Azure AI Search RAG

Use Case 6 differs from the simulated RAG demo (Use Case 4) by connecting to a **real Azure AI Search index**:

- The agent is created with an `AzureAISearchToolResource` pointing to your live index.
- Queries use the **Semantic** query type for keyword search with semantic reranking.
- The agent grounds its answers in retrieved documents and includes **citations** with source titles and URLs.
- Supports fully interactive multi-turn chat — type `exit` or `quit` to end.

> **Note:** The query type can be adjusted depending on your index configuration:
> - `Simple` — keyword search only
> - `Semantic` — keyword + semantic reranking (requires a semantic configuration on the index)
> - `VectorSimpleHybrid` — vector + keyword (requires vector fields with an integrated vectorizer)
> - `VectorSemanticHybrid` — vector + keyword + semantic reranking (requires both)

### Use Case 7 – Multi-Agent Orchestration

Demonstrates a **sequential agent pipeline**:
1. A **Researcher** agent gathers structured research notes on a topic.
2. The research output is passed to a **Writer** agent that produces a polished article.
3. Shows how to orchestrate multiple agents without requiring Semantic Kernel.

### Use Case 8 – File Search (Vector Store)

- Uploads 3 sample Markdown files (product spec, HR policy, sales report) via `client.Files.UploadFileAsync`.
- Creates a **vector store**, indexes documents, and enables the agent to search them.
- Uses the built-in `FileSearchToolDefinition` — different from Azure AI Search (Use Case 6).

### Use Case 14 – Structured Output

Uses `response_format: json_schema` with strict mode to force the agent to return valid JSON matching a predefined schema. Three practical examples:
- **Review Analyzer** — sentiment, pros/cons, rating
- **Contact Extractor** — names, emails, phones from unstructured text
- **Code Reviewer** — issues, scores, refactoring suggestions

## Authentication

The app uses `DefaultAzureCredential`, which tries (in order):
- Environment variables
- Managed Identity
- Azure CLI (`az login`)
- Visual Studio / VS Code credentials

Make sure you're signed in with `az login` or have the appropriate environment set up.

## License

MIT

# Design — BotPulse Intelligence Platform

## Status
In Progress (Phase 3)

**ADR:** ADR-016 · **Depends on:** ADR-017 contracts

---

## 1. Project Structure

Three new projects with a strict dependency direction. Only `Implementations` references external AI SDKs.

```
src/
├── BotPulse.Intelligence.Contracts/      # Pure abstractions, no external deps
│   ├── AI/
│   │   ├── IChatCompletionProvider.cs
│   │   ├── IEmbeddingProvider.cs
│   │   ├── ChatMessage.cs / ChatRequest.cs / ChatResponse.cs
│   │   └── EmbeddingResult.cs
│   ├── Vector/
│   │   ├── IVectorStore.cs
│   │   ├── VectorRecord.cs
│   │   ├── VectorQuery.cs
│   │   └── VectorSearchResult.cs
│   ├── Knowledge/
│   │   ├── IKnowledgeBase.cs
│   │   ├── KnowledgeItem.cs
│   │   └── KnowledgeSourceType.cs
│   ├── Agents/
│   │   ├── IAgent.cs
│   │   ├── IAgentRegistry.cs
│   │   ├── AgentRequest.cs / AgentResult.cs
│   │   └── IAgentMemory.cs
│   ├── Tools/
│   │   ├── ITool.cs
│   │   ├── IToolRegistry.cs
│   │   └── ToolContext.cs / ToolResult.cs
│   ├── Diagnostics/
│   │   ├── IDiagnosticService.cs
│   │   ├── DiagnosisRequest.cs
│   │   └── DiagnosisResult.cs
│   └── Configuration/
│       └── IntelligenceOptions.cs
│
├── BotPulse.Intelligence/                 # Core domain logic (orchestration, pipelines)
│   ├── Knowledge/KnowledgeBaseService.cs
│   ├── Diagnostics/RagDiagnosticService.cs
│   ├── Agents/AgentRegistry.cs
│   ├── Agents/DiagnosticAgent.cs
│   ├── Tools/ToolRegistry.cs
│   └── DependencyInjection/IntelligenceServiceCollectionExtensions.cs
│
└── BotPulse.Intelligence.Providers/       # External implementations (SK, OpenAI, pgvector)
    ├── SemanticKernel/SemanticKernelChatProvider.cs
    ├── OpenAI/OpenAIEmbeddingProvider.cs
    ├── Ollama/OllamaChatProvider.cs
    ├── PgVector/PgVectorStore.cs
    ├── InMemory/InMemoryVectorStore.cs    # for tests / dev
    └── DependencyInjection/ProvidersServiceCollectionExtensions.cs
```

### Dependency direction

```
BotPulse.Core
     ▲
     │ (domain refs only)
BotPulse.Intelligence.Contracts
     ▲                    ▲
     │                    │
BotPulse.Intelligence   BotPulse.Intelligence.Providers
     ▲                    ▲
     └────────┬───────────┘
        BotPulse.Api / Worker (composition root)
```

The API/Worker is the only composition root that references `Providers` — everything else depends on `Contracts`.

---

## 2. Core Abstractions (Contracts)

### AI providers

```csharp
public interface IChatCompletionProvider
{
    string Name { get; }
    Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamAsync(ChatRequest request, CancellationToken ct = default);
}

public interface IEmbeddingProvider
{
    string Name { get; }
    int Dimensions { get; }
    Task<EmbeddingResult> EmbedAsync(string text, CancellationToken ct = default);
    Task<IReadOnlyList<EmbeddingResult>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default);
}
```

### Vector store

```csharp
public interface IVectorStore
{
    Task UpsertAsync(VectorRecord record, CancellationToken ct = default);
    Task UpsertBatchAsync(IReadOnlyList<VectorRecord> records, CancellationToken ct = default);
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(VectorQuery query, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

public sealed record VectorRecord(
    string Id,
    float[] Embedding,
    string Content,
    IReadOnlyDictionary<string, string> Metadata,
    Guid? OrganizationId = null);       // multi-tenant ready

public sealed record VectorQuery(
    float[] Embedding,
    int TopK = 5,
    double MinScore = 0.7,
    Guid? OrganizationId = null,        // tenant scoping
    IReadOnlyDictionary<string, string>? MetadataFilter = null);

public sealed record VectorSearchResult(string Id, string Content, double Score,
    IReadOnlyDictionary<string, string> Metadata);
```

### Knowledge base

```csharp
public interface IKnowledgeBase
{
    Task IndexAsync(KnowledgeItem item, CancellationToken ct = default);
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string query, int topK = 5,
        Guid? organizationId = null, CancellationToken ct = default);
}

public sealed record KnowledgeItem(
    string Id,                          // deterministic → idempotent indexing
    KnowledgeSourceType SourceType,
    string Content,
    IReadOnlyDictionary<string, string> Metadata,
    Guid? OrganizationId = null);

public enum KnowledgeSourceType
{
    ExecutionLog, Exception, Alert, Runbook, HistoricalIncident, Resolution
}
```

### Diagnostic service (MVP capability)

```csharp
public interface IDiagnosticService
{
    Task<DiagnosisResult> DiagnoseAsync(DiagnosisRequest request, CancellationToken ct = default);
}

public sealed record DiagnosisRequest(
    string JobId, string ErrorMessage, string? StackTrace,
    string? ProcessName, string? RobotName, Guid? OrganizationId = null);

public sealed record DiagnosisResult(
    string RootCause, string Impact, IReadOnlyList<string> ResolutionSteps,
    double Confidence, IReadOnlyList<string> ReferencedKnowledgeIds);
```

### Agent & tool framework

```csharp
public interface IAgent
{
    string Name { get; }
    Task<AgentResult> RunAsync(AgentRequest request, CancellationToken ct = default);
}

public interface IAgentRegistry
{
    void Register(IAgent agent);
    IAgent? Resolve(string name);
    IReadOnlyCollection<IAgent> All { get; }
}

public interface ITool
{
    string Name { get; }
    string Description { get; }
    Task<ToolResult> InvokeAsync(ToolContext context, CancellationToken ct = default);
}

public interface IToolRegistry
{
    void Register(ITool tool);
    ITool? Resolve(string name);
    IReadOnlyCollection<ITool> All { get; }
}
```

---

## 3. Implementations (Providers)

| Abstraction | MVP implementation | Future implementations |
|-------------|-------------------|------------------------|
| `IChatCompletionProvider` | `SemanticKernelChatProvider` (wraps SK → OpenAI/Ollama) | Anthropic, Bedrock |
| `IEmbeddingProvider` | `OpenAIEmbeddingProvider` / `OllamaEmbeddingProvider` | Azure, local models |
| `IVectorStore` | `PgVectorStore` (PostgreSQL + pgvector), `InMemoryVectorStore` (tests) | Pinecone, Qdrant, Weaviate |

**Semantic Kernel constraint:** SK types appear ONLY inside `BotPulse.Intelligence.Providers`. No SK type crosses the `Contracts` boundary.

---

## 4. Provider Selection (Configuration)

```json
{
  "Intelligence": {
    "ChatProvider": "Ollama",          // Ollama | OpenAI | Anthropic
    "EmbeddingProvider": "Ollama",
    "VectorStore": "PgVector",         // PgVector | InMemory
    "MaxContextTokens": 4000,
    "OpenAI":    { "ApiKey": "", "ChatModel": "gpt-4o-mini", "EmbeddingModel": "text-embedding-3-small" },
    "Ollama":    { "Endpoint": "http://localhost:11434", "ChatModel": "llama3", "EmbeddingModel": "nomic-embed-text" },
    "PgVector":  { "ConnectionString": "" }
  }
}
```

DI reads `Intelligence:ChatProvider` etc. and registers the matching implementation. Adding a provider = new class + one `switch` arm.

---

## 5. Persistence (Independent)

Own schema (separate tables, MVP in the same PostgreSQL instance with `pgvector`):

```sql
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE ai_knowledge (
    id              TEXT PRIMARY KEY,          -- deterministic (idempotent)
    source_type     TEXT NOT NULL,
    content         TEXT NOT NULL,
    embedding       vector(1536),              -- dimension per embedding model
    metadata        JSONB NOT NULL DEFAULT '{}',
    organization_id UUID,                       -- multi-tenant ready
    created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_ai_knowledge_embedding ON ai_knowledge
    USING ivfflat (embedding vector_cosine_ops);
CREATE INDEX idx_ai_knowledge_org ON ai_knowledge (organization_id);

CREATE TABLE ai_diagnosis_feedback (
    id              BIGSERIAL PRIMARY KEY,
    job_id          TEXT NOT NULL,
    diagnosis       JSONB NOT NULL,
    validated       BOOLEAN,                    -- null = pending, true/false = operator signal
    created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

> Vector dimension is configurable to match the embedding model (1536 for OpenAI small, 768 for nomic-embed).

---

## 6. RAG Diagnostic Pipeline (MVP flow)

```
JobFailedEvent (or manual request)
        │
        ▼
IDiagnosticService.DiagnoseAsync
        │  1. embed(errorMessage + stackTrace)  → IEmbeddingProvider
        │  2. vector search topK, tenant-scoped → IVectorStore
        │  3. build prompt with retrieved context (truncated to MaxContextTokens)
        │  4. IChatCompletionProvider.CompleteAsync  (async, streamed to UI via SSE)
        │  5. parse → DiagnosisResult
        │  6. persist diagnosis + await operator feedback
        ▼
   DiagnosisResult (root cause, impact, steps, confidence)
```

---

## 7. Security & Authorization

- `IDiagnosticService` / agents receive `AuthorizationContext` (ADR-017) via `IAuthorizationContextAccessor`.
- Permission check before accessing operational data — new permission `Intelligence.View` / `Intelligence.Diagnose` (added to `PermissionCatalog`).
- Vector queries always pass `OrganizationId` from the context → tenant isolation.
- Every diagnosis / tool invocation writes an `AuditRecordData` (reuse existing `IAuditRepository`).
- No autonomous execution — corrective actions require human approval (Requisito 9).

---

## 8. Integration Points

| Concern | Approach |
|---------|----------|
| Trigger | Subscribe to `JobFailedEvent` (event-driven) + manual `POST /api/v1/intelligence/diagnose/{jobId}` |
| UI | "AI Analysis" panel on the Job detail page; streamed via SSE |
| Composition root | API/Worker registers `AddIntelligence()` + `AddIntelligenceProviders(config)` |
| Core coupling | Core only knows `IDiagnosticService` (in Contracts) — never a provider |

---

## 9. MVP Scope vs Deferred

**In MVP (Phase 3A/3B):**
- Contracts + Core + Providers projects
- Provider abstractions (chat, embedding, vector)
- PgVector + InMemory vector stores
- Knowledge base indexing (logs, exceptions, resolutions)
- RAG diagnostic service + API endpoint + UI panel
- Security enforcement + audit

**Deferred (later sub-phases):**
- Full agent framework wiring (agents beyond DiagnosticAgent)
- NL-to-Query command bar
- Self-healing agent
- Prediction engine / anomaly detection
- Standalone service extraction

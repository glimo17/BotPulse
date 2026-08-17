# Tasks — BotPulse Intelligence Platform

## Status
Ready for Implementation

**ADR:** ADR-016 · **Design:** design.md · **Requirements:** requirements.md

---

## Milestone 1 — Project Scaffolding & Contracts

**Goal:** Create the 3 projects with the dependency direction and all abstractions (no external deps in Contracts). Everything compiles, nothing wired yet.

- **T1.1** Create `BotPulse.Intelligence.Contracts` project; reference only `BotPulse.Core`. Add to solution.
- **T1.2** Create `BotPulse.Intelligence` project; reference `Contracts`. Add to solution.
- **T1.3** Create `BotPulse.Intelligence.Providers` project; reference `Contracts` + `Intelligence`. Add to solution.
- **T1.4** Define AI abstractions: `IChatCompletionProvider`, `IEmbeddingProvider` + DTOs (`ChatMessage`, `ChatRequest`, `ChatResponse`, `EmbeddingResult`).
- **T1.5** Define vector abstractions: `IVectorStore`, `VectorRecord`, `VectorQuery`, `VectorSearchResult`.
- **T1.6** Define knowledge abstractions: `IKnowledgeBase`, `KnowledgeItem`, `KnowledgeSourceType`.
- **T1.7** Define diagnostic abstractions: `IDiagnosticService`, `DiagnosisRequest`, `DiagnosisResult`.
- **T1.8** Define agent/tool abstractions: `IAgent`, `IAgentRegistry`, `IAgentMemory`, `ITool`, `IToolRegistry`, contexts/results.
- **T1.9** Define `IntelligenceOptions` configuration model.
- **T1.10** Verify solution builds clean.

---

## Milestone 2 — Vector Store & Embeddings

**Goal:** Working vector storage + embedding generation, testable without external LLM.

- **T2.1** Implement `InMemoryVectorStore` (cosine similarity) in Providers — for tests/dev.
- **T2.2** Implement `PgVectorStore` (PostgreSQL + pgvector) with tenant-scoped queries.
- **T2.3** EF/SQL migration for `ai_knowledge` table (vector column, ivfflat index, org index).
- **T2.4** Implement embedding providers: `OllamaEmbeddingProvider` and `OpenAIEmbeddingProvider`.
- **T2.5** Embedding cache (identical text → cached embedding) per NFR-01.
- **T2.6** Unit tests: InMemory vector store search ranking + embedding cache hit.

---

## Milestone 3 — Knowledge Base & Indexing

**Goal:** Index operational content into the vector store, idempotently.

- **T3.1** Implement `KnowledgeBaseService` (embed + upsert + search).
- **T3.2** Deterministic ID generation for idempotent indexing.
- **T3.3** Token truncation before embedding (configurable `MaxContextTokens`).
- **T3.4** Indexers for execution logs, exceptions, and validated resolutions.
- **T3.5** Unit tests: re-indexing same item does not duplicate; truncation applied.

---

## Milestone 4 — RAG Diagnostic Service

**Goal:** The MVP capability — diagnose a failed job via RAG.

- **T4.1** Implement `SemanticKernelChatProvider` (wraps SK → Ollama/OpenAI) in Providers.
- **T4.2** Implement `RagDiagnosticService`: embed → search → build prompt → complete → parse.
- **T4.3** Prompt template for diagnosis (root cause, impact, resolution steps, confidence).
- **T4.4** LLM result cache for identical prompts (NFR-01).
- **T4.5** Low-confidence path when no relevant knowledge found.
- **T4.6** Unit tests with mocked chat + in-memory vector store.

---

## Milestone 5 — Security, API & DI Wiring

**Goal:** Expose the capability securely and wire the composition root.

- **T5.1** Add `Intelligence.View` + `Intelligence.Diagnose` permissions to `PermissionCatalog`; seed to Administrator.
- **T5.2** `IntelligenceServiceCollectionExtensions.AddIntelligence()` (Core services).
- **T5.3** `ProvidersServiceCollectionExtensions.AddIntelligenceProviders(config)` — provider selection by config.
- **T5.4** `IntelligenceController` — `POST /api/v1/intelligence/diagnose/{jobId}` gated by `Intelligence.Diagnose`.
- **T5.5** Authorization context + tenant scoping + audit on every diagnosis.
- **T5.6** Register `AddIntelligence` + `AddIntelligenceProviders` in API composition root.

---

## Milestone 6 — Event-Driven Trigger & UI

**Goal:** Auto-diagnose on failure and surface it to operators.

- **T6.1** Subscribe to `JobFailedEvent` to trigger async diagnosis (Worker).
- **T6.2** Stream diagnosis to UI via SSE.
- **T6.3** "AI Analysis" panel on the Job detail page.
- **T6.4** Operator feedback (validate / reject) → persist to `ai_diagnosis_feedback`.
- **T6.5** i18n keys (EN + ES) for the AI panel.

---

## Definition of Done

- [ ] `BotPulse.Core` references no external AI SDK, vector driver, or SK type
- [ ] No SK / provider type crosses the `Contracts` boundary
- [ ] Provider selection works purely via configuration
- [ ] RAG diagnostic pipeline produces a result end-to-end with Ollama locally
- [ ] Vector queries are tenant-scoped; diagnoses are audited
- [ ] No autonomous execution — corrective actions require human approval
- [ ] Unit tests pass with in-memory vector store (no external DB required)

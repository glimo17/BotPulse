# Requisitos — BotPulse Cognitive & RAG Engine

## Estado: Planificado (Fase 5)

Este documento captura los requisitos para implementación futura. No se ha iniciado la implementación.

---

## FR-01: Gestión de Embeddings y Base de Conocimiento Vectorial

**Historia de Usuario:** Como plataforma, quiero almacenar embeddings de errores, selectores y resoluciones, para construir una base de conocimiento semántica que mejore con el tiempo.

### Criterios de Aceptación

1. THE **Vector Store** SHALL use PostgreSQL with the `pgvector` extension for storage and cosine similarity search.
2. THE **Embedding Pipeline** SHALL automatically vectorize three types of operational artifacts: execution error traces (`ExecutionLogSnapshot` with severity Error), failed selector descriptions, and validated resolution records.
3. WHEN a new artifact is persisted, THE **Embedding Service** SHALL generate its embedding vector asynchronously using the configured `IEmbeddingProvider` and store it in the vector table.
4. THE **Vector Table** SHALL include columns: `id`, `organization_id`, `artifact_type`, `artifact_id`, `embedding` (vector), `content_text`, `metadata_json`, `created_at_utc`.
5. THE **Vector Search** SHALL support filtering by `organization_id` and `artifact_type` before applying cosine similarity.

---

## FR-02: Recuperación Semántica de Contexto (Pipeline RAG)

**Historia de Usuario:** Como sistema de diagnóstico, quiero recuperar incidentes históricos similares al error actual, para inyectar contexto relevante en el prompt del LLM.

### Criterios de Aceptación

1. WHEN a Job transitions to `Failed`, THE **RAG Pipeline** SHALL query the vector store for the top-K (configurable, default 5) most similar historical incidents.
2. THE **RAG Pipeline** SHALL compute similarity using cosine distance on the embedding of the current error trace.
3. THE **Context Builder** SHALL format the retrieved incidents as structured context for the LLM prompt, including: original error, resolution applied, outcome.
4. THE **RAG Pipeline** SHALL apply the organization_id filter to ensure tenant isolation.
5. IF no similar incidents exist (similarity < configurable threshold, default 0.7), THEN THE **RAG Pipeline** SHALL proceed with LLM diagnosis without historical context.

---

## FR-03: Diagnóstico Asistido y Causa Raíz en Lenguaje Natural

**Historia de Usuario:** Como operador, quiero ver un diagnóstico automático de cada job fallido en lenguaje natural, para reducir el tiempo de investigación.

### Criterios de Aceptación

1. THE **AI Diagnosis Panel** SHALL appear in the Job Detail view for jobs with `Status == Failed`.
2. THE **Diagnosis** SHALL include three sections: Causa Técnica, Impacto Estimado, Pasos Recomendados.
3. WHEN the diagnosis is generated, THE **AI Service** SHALL use the RAG context plus the current error data to produce the response.
4. THE **Diagnosis Generation** SHALL be asynchronous — the job detail view loads immediately and the diagnosis appears when ready.
5. THE **AI Diagnosis Panel** SHALL show a loading indicator while the diagnosis is being generated.
6. WHEN the diagnosis is ready, THE **System** SHALL cache it associated with the job ID.

---

## FR-04: Búsqueda Operacional en Lenguaje Natural (NL-to-Query)

**Historia de Usuario:** Como operador, quiero buscar información escribiendo en lenguaje natural, para no tener que recordar filtros complejos.

### Criterios de Aceptación

1. THE **NL Search Bar** SHALL accept natural language queries in Spanish and English.
2. WHEN a query is submitted, THE **NL-to-Query Translator** SHALL use the configured `IAIService` to convert it into structured API filter parameters.
3. THE **NL Search Bar** SHALL display the interpreted filters before executing, allowing the user to adjust.
4. THE **NL-to-Query Translator** SHALL support queries about: jobs, robots, alerts, logs, queues.
5. IF the query cannot be interpreted, THEN THE **NL Search Bar** SHALL show an informative message.

---

## FR-05: Agente de Auto-Reparación (Self-Healing Bots)

**Historia de Usuario:** Como plataforma, quiero detectar selectores rotos y sugerir parches automáticamente, para reducir el mantenimiento correctivo más costoso en RPA.

### Criterios de Aceptación

1. WHEN a job fails with an error related to selectors (detectado por pattern matching en el ErrorMessage), THE **Self-Healing Agent** SHALL analyze the discrepancy.
2. THE **Self-Healing Agent** SHALL generate a suggested patch (new XPath/CSS selector) based on the current page structure and historical patterns.
3. THE **Suggested Patch** SHALL require explicit human approval before being applied — never automatic.
4. WHEN an operator approves a patch, THE **System** SHALL record the resolution and vectorize it for future RAG queries.
5. THE **Self-Healing Agent** SHALL show a confidence score (0-100%) for each suggested patch.

---

## FR-06: Memoria de Aprendizaje Continuo (Feedback Loop)

**Historia de Usuario:** Como plataforma, quiero aprender de cada resolución validada por operadores, para mejorar la calidad de diagnósticos futuros.

### Criterios de Aceptación

1. WHEN an operator marks an AI diagnosis as "útil" or validates a resolution, THE **Feedback Service** SHALL vectorize the (Problem → Solution) pair and store it in the vector database.
2. WHEN an operator marks an AI diagnosis as "no útil", THE **Feedback Service** SHALL record negative feedback for model evaluation.
3. THE **Learning Pipeline** SHALL run periodically to re-embed outdated resolutions with updated embedding models.
4. THE **System** SHALL track a "knowledge base health" metric: total entries, average usefulness rating, coverage by process.

---

## FR-07: Motor de Predicción de Anomalías

**Historia de Usuario:** Como operador, quiero recibir alertas proactivas antes de que un proceso falle, basadas en desviaciones estadísticas de su comportamiento histórico.

### Criterios de Aceptación

1. THE **Anomaly Detection Service** SHALL compute rolling statistics (mean, std deviation) for each process's execution duration and success rate over configurable windows (default: 7 days).
2. WHEN the current execution metrics deviate beyond a configurable threshold (default: 2 standard deviations), THE **Anomaly Detector** SHALL raise a proactive alert.
3. THE **Proactive Alert** SHALL include: affected process, current value, expected range, deviation magnitude, and trend direction.
4. THE **Anomaly Detection Service** SHALL integrate with the existing Alert Engine as a new `IAlertRuleEvaluator` implementation (`AnomalyDetectionEvaluator`).
5. THE **Anomaly Detection** SHALL NOT require ML model training — it uses statistical methods (Z-score, IQR) for simplicity and interpretability.

---

## NFR-01: Desacoplamiento de Proveedores de LLM

### Criterios de Aceptación

1. THE **Core** SHALL define `IAIService` interface with methods: `GenerateDiagnosisAsync`, `TranslateNLQueryAsync`, `GenerateSelectorPatchAsync`.
2. THE **Core** SHALL define `IEmbeddingProvider` interface with method: `GenerateEmbeddingAsync(text) → float[]`.
3. THE **Core** SHALL define `IVectorSearchRepository` interface with methods: `SearchSimilarAsync`, `StoreAsync`, `DeleteAsync`.
4. Concrete implementations SHALL live in separate projects: `BotPulse.Cognitive.OpenAI`, `BotPulse.Cognitive.Anthropic`, `BotPulse.Cognitive.Ollama`.
5. Provider selection SHALL be by configuration: `AI__Provider=OpenAI|Anthropic|Ollama`.

---

## NFR-02: Control de Costos y Tasa de Tokens

### Criterios de Aceptación

1. THE **Token Manager** SHALL truncate input context to a configurable maximum (default: 4000 tokens) before sending to the LLM API.
2. THE **Embedding Cache** SHALL store generated embeddings and reuse them for identical content (content hash as key).
3. THE **Rate Limiter** SHALL enforce a configurable max requests per minute to the LLM API (default: 60 rpm).
4. THE **Cost Tracker** SHALL log estimated token usage and cost per request for monitoring.

---

## NFR-03: Aislamiento Multi-Tenant del Espacio Vectorial

### Criterios de Aceptación

1. ALL vector store queries SHALL include `organization_id` as a mandatory filter — no cross-tenant data leakage.
2. THE **Vector Table Schema** SHALL use `organization_id` as part of a composite index alongside the vector column.
3. THE **Embedding Pipeline** SHALL tag every stored vector with the `organization_id` of the originating job/process.
4. THE **Multi-Tenant Isolation** SHALL be enforced at the repository level, not at the application service level (defense in depth).

---

## Dependencias

| Dependencia | Razón |
|---|---|
| Fase 4 (Multi-Tenant) | Requerido para NFR-03: aislamiento vectorial por organización |
| PostgreSQL 15+ con pgvector | Almacenamiento vectorial en la misma instancia de DB |
| API key de LLM provider | OpenAI, Anthropic o modelo local vía Ollama |

---

## Notas de Arquitectura Propuesta

```
BotPulse.Cognitive/                  ← Nuevo proyecto
├── Abstractions/
│   ├── IAIService.cs
│   ├── IEmbeddingProvider.cs
│   └── IVectorSearchRepository.cs
├── Services/
│   ├── RAGPipeline.cs
│   ├── DiagnosisService.cs
│   ├── NLQueryTranslator.cs
│   ├── SelfHealingAgent.cs
│   ├── FeedbackService.cs
│   └── AnomalyDetectionEvaluator.cs
└── DependencyInjection/
    └── CognitiveServiceRegistration.cs

BotPulse.Cognitive.OpenAI/           ← Provider concreto
├── OpenAIService.cs
├── OpenAIEmbeddingProvider.cs
└── DependencyInjection/

BotPulse.Cognitive.Ollama/           ← Provider local
├── OllamaService.cs
├── OllamaEmbeddingProvider.cs
└── DependencyInjection/
```

El `IVectorSearchRepository` se implementa en `BotPulse.Infrastructure` usando EF Core + pgvector.

# Requisitos — BotPulse Intelligence Platform

## Estado: Planificado (Fase 3)

**ADR de referencia:** ADR-016 (BotPulse Intelligence Platform)
**Depende de:** ADR-017 (contratos de autorización: `IAuthorizationService`, `IAuthorizationContextAccessor`, `AuthorizationContext`)

---

## Introducción

BotPulse incorpora una plataforma de inteligencia artificial (`BotPulse.Intelligence`) completamente desacoplada del Core operacional. La plataforma soporta múltiples capacidades de IA (RAG, agents, tools, predicción) detrás de abstracciones, sin acoplar el Core a ningún proveedor de LLM, embeddings, vector store o framework de orquestación.

El MVP de Fase 3 se enfoca en la **fundación arquitectónica** y el primer caso de uso concreto: **diagnóstico asistido de jobs fallidos** mediante un pipeline RAG.

---

## Glosario

| Término | Definición |
|---------|-----------|
| **Intelligence Platform** | Módulo `BotPulse.Intelligence` que agrupa todas las capacidades de IA |
| **LLM** | Large Language Model (OpenAI, Anthropic, Ollama, etc.) |
| **Embedding** | Representación vectorial de texto para búsqueda semántica |
| **Vector Store** | Almacenamiento especializado para embeddings con búsqueda por similitud |
| **RAG** | Retrieval-Augmented Generation — recuperar contexto relevante e inyectarlo al LLM |
| **Agent** | Componente de IA que ejecuta una tarea usando tools y memoria |
| **Tool** | Operación acotada que un agent puede invocar (leer logs, consultar DB, etc.) |
| **Knowledge Source** | Fuente de datos indexada (logs, alertas, runbooks, incidentes históricos) |
| **Orchestration Framework** | Framework interno (Semantic Kernel en MVP) oculto tras abstracciones |

---

## Sección 1: Fundación y Desacoplamiento

### Requisito 1: Módulo Independiente

**Historia de Usuario:** Como arquitecto, quiero que la plataforma de IA sea un módulo independiente, para que evolucione sin acoplar el Core operacional.

#### Criterios de Aceptación

1. THE **system** SHALL introduce a `BotPulse.Intelligence` set of projects independent from `BotPulse.Core`.
2. THE **BotPulse.Core** SHALL NOT reference any LLM SDK, embedding provider, vector database driver, or orchestration framework.
3. THE **Intelligence Platform** SHALL communicate with the rest of BotPulse exclusively through interfaces/contracts.
4. THE **Intelligence Platform** SHALL be replaceable without modifying `BotPulse.Core`, the API, or the Worker.
5. THE **contracts project** (`BotPulse.Intelligence.Contracts`) SHALL contain no external dependencies beyond `BotPulse.Core` domain references.

---

### Requisito 2: Provider Pattern en Todas las Dependencias Externas

**Historia de Usuario:** Como arquitecto, quiero que cada dependencia externa de IA esté abstraída, para cambiar de proveedor sin reescribir lógica.

#### Criterios de Aceptación

1. THE **platform** SHALL define an abstraction for chat completion (LLM) so implementations (OpenAI, Anthropic, Ollama) are interchangeable.
2. THE **platform** SHALL define an abstraction for embedding generation independent of the provider.
3. THE **platform** SHALL define an abstraction for vector storage so implementations (pgvector, Pinecone, Qdrant, etc.) are interchangeable.
4. THE **platform** SHALL select the active provider through configuration, without code changes.
5. THE rest of BotPulse SHALL NEVER reference concrete provider types (OpenAI, pgvector, Semantic Kernel, etc.) — only abstractions.
6. WHEN a new provider is added, THE **platform** SHALL require only a new implementation class plus a DI registration.

---

## Sección 2: Persistencia Independiente

### Requisito 3: Base de Datos Propia

**Historia de Usuario:** Como arquitecto, quiero que la IA gestione su propia persistencia, para aislar su ciclo de vida de datos del Core.

#### Criterios de Aceptación

1. THE **Intelligence Platform** SHALL NOT depend on the operational BotPulse database schema for its own data.
2. THE **platform** SHALL own its persistence: vector storage, knowledge records, prompt templates, AI result cache, and conversation history.
3. THE **platform** SHALL support PostgreSQL + `pgvector` as the MVP vector store implementation.
4. THE **platform data model** SHALL include a nullable `organization_id` on all tables to support future multi-tenant isolation without schema migration.

---

## Sección 3: Knowledge Base & RAG

### Requisito 4: Indexación de Múltiples Fuentes

**Historia de Usuario:** Como operador, quiero que el sistema aprenda de logs, alertas y resoluciones pasadas, para diagnosticar fallos más rápido.

#### Criterios de Aceptación

1. THE **Knowledge Base** SHALL support indexing content from multiple source types: execution logs, exceptions, alerts, runbooks, historical incidents, and validated resolutions.
2. WHEN a knowledge item is indexed, THE **system** SHALL generate an embedding and store it in the vector store with metadata (source type, resource id, timestamp).
3. THE **indexing pipeline** SHALL be idempotent — re-indexing the same item SHALL NOT create duplicates.
4. THE **system** SHALL truncate content that exceeds the configured token limit before embedding.

### Requisito 5: Pipeline de Diagnóstico RAG (MVP)

**Historia de Usuario:** Como operador, quiero un diagnóstico automático de un job fallido, para reducir el tiempo de resolución.

#### Criterios de Aceptación

1. WHEN a diagnosis is requested for a failed job, THE **system** SHALL retrieve semantically similar knowledge items from the vector store.
2. THE **system** SHALL inject the retrieved context into the LLM prompt and generate a diagnosis containing: probable root cause, impact, and suggested resolution steps.
3. THE **diagnosis** SHALL be generated asynchronously and SHALL NOT block the job detail view.
4. IF no relevant knowledge is found, THE **system** SHALL still produce a best-effort diagnosis and indicate low confidence.
5. THE **system** SHALL record a feedback signal (validated / rejected) from operators to improve future retrievals.

---

## Sección 4: Agent & Tool Framework

### Requisito 6: Agent Framework Genérico

**Historia de Usuario:** Como plataforma, quiero un framework de agents reutilizable, para añadir nuevos agents sin reescribir la orquestación.

#### Criterios de Aceptación

1. THE **platform** SHALL expose a generic agent abstraction capable of hosting specialized agents (diagnostic, monitoring, recommendation, self-healing, optimization).
2. Agents SHALL share common infrastructure: state management, memory context, tool invocation, and result tracking.
3. THE **platform** SHALL allow registering a new agent type without modifying existing agents.

### Requisito 7: Tool Framework

**Historia de Usuario:** Como plataforma, quiero que los agents interactúen mediante tools acotadas, para controlar y auditar lo que la IA puede hacer.

#### Criterios de Aceptación

1. THE **platform** SHALL define a tool abstraction that agents invoke to interact with BotPulse data.
2. Agents SHALL NEVER access operational infrastructure directly — only through registered tools.
3. THE **tool registry** SHALL allow registering new tools without modifying the agent framework.
4. EACH tool invocation SHALL be auditable.

---

## Sección 5: Seguridad y Gobernanza

### Requisito 8: Enforcement de Autorización

**Historia de Usuario:** Como responsable de seguridad, quiero que la IA respete las reglas de autorización, para que no se convierta en un backdoor.

#### Criterios de Aceptación

1. THE **Intelligence Platform** SHALL consume `AuthorizationContext` from ADR-017 and SHALL NOT implement its own user or permission model.
2. THE **platform** SHALL validate the caller's permissions before accessing operational data.
3. THE **platform** SHALL scope vector store queries by tenant/organization to prevent cross-tenant knowledge leakage.
4. ALL AI operations that access or process customer data SHALL be recorded in the audit log.
5. THE **platform** SHALL NEVER bypass BotPulse authorization rules.

### Requisito 9: Human-in-the-Loop

**Historia de Usuario:** Como operador, quiero aprobar cualquier acción correctiva generada por IA, para evitar automatización insegura.

#### Criterios de Aceptación

1. THE **platform** MAY recommend, simulate, or prepare corrective actions.
2. THE **platform** SHALL NOT execute AI-generated operational changes autonomously.
3. WHEN an agent proposes a corrective action, THE **system** SHALL require explicit human approval before any execution.

---

## Sección 6: Deployment

### Requisito 10: Despliegue Flexible

**Historia de Usuario:** Como operador de infraestructura, quiero desplegar la IA como parte del monolito o como servicio independiente, sin cambiar código.

#### Criterios de Aceptación

1. THE **Intelligence Platform** SHALL run embedded in the existing API/Worker for the MVP.
2. THE **architecture** SHALL allow extracting the platform into a standalone ASP.NET service in the future without code changes to the Core.
3. Configuration SHALL rely exclusively on environment variables / configuration providers.

---

## Requisitos No Funcionales

### NFR-01: Control de Costos
- Log truncation before embedding (configurable max tokens)
- Embedding cache — identical text SHALL NOT be re-embedded
- LLM result cache for identical prompts
- Configurable rate limiting on external AI providers

### NFR-02: Latencia
- Diagnoses SHALL be generated asynchronously
- Target: first result in < 10s for the diagnostic pipeline

### NFR-03: Testabilidad
- All provider abstractions SHALL be mockable
- The RAG pipeline SHALL be testable with an in-memory vector store (no external DB)

### NFR-04: Extensibilidad
- Adding a new AI provider, vector store, agent type, or tool SHALL require minimal or zero changes to existing code

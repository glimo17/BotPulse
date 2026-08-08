# ADR-016: BotPulse Intelligence Platform

## Status
Proposed

**Target Implementation:** Phase 3 - Intelligence Platform Foundation

**Related ADRs:** ADR-017 (Identity, Authentication & Authorization — provides the authorization context consumed by this platform)

---

## Context

Las plataformas RPA generan grandes volúmenes de datos operacionales: logs de ejecución, trazas de errores, alertas, métricas y resoluciones manuales por operadores. Este conocimiento permanece disperso y no se reutiliza sistemáticamente.

Los operadores dedican tiempo significativo a:
- Diagnosticar causas raíz manualmente leyendo logs extensos
- Buscar si un error similar ya ocurrió y cómo se resolvió
- Corregir automatizaciones rotas cuando las aplicaciones target cambian
- Identificar degradaciones de rendimiento antes de que impacten las operaciones

La industria ha demostrado que técnicas de IA como RAG, agents con herramientas, y análisis predictivo pueden automatizar gran parte de este trabajo operacional.

### Problema Arquitectónico

¿Cómo incorporamos capacidades de IA en BotPulse sin acoplar el Core a proveedores específicos de LLM, vectorización, o frameworks de orchestration? ¿Cómo diseñamos para que agregar nuevas capacidades de IA no requiera modificar la arquitectura existente?

---

## Decision

Se introduce un módulo `BotPulse.Intelligence` como una **Intelligence Platform** completa, independiente y extensible.

### Architecture Overview

```
┌──────────────────────────────────────────────────────┐
│              Identity Layer  (ADR-017)               │
│         User Management / Credentials                │
└─────────────────────┬────────────────────────────────┘
                      │
┌─────────────────────▼────────────────────────────────┐
│           Authentication Layer  (ADR-017)            │
│    IAuthenticationProvider (Local / Entra / LDAP)   │
└─────────────────────┬────────────────────────────────┘
                      │
┌─────────────────────▼────────────────────────────────┐
│           Authorization / RBAC  (ADR-017)            │
│     IAuthorizationService / AuthorizationContext     │
└──────────┬──────────────────────────┬────────────────┘
           │ Authorization Context    │ Authorization Context
           │                          │
┌──────────▼──────────┐   ┌──────────▼──────────────────┐
│   BotPulse Core     │   │  BotPulse Intelligence      │
│   (Operational)     │   │  Platform                   │
│                     │   │                             │
│                     │   │  ┌─────────────────────┐   │
│  Domain Events  ────────►  │   Agent Framework   │   │
│                     │   │  └─────────────────────┘   │
│                     │   │  ┌─────────────────────┐   │
│                     │   │  │   RAG Pipeline      │   │
│                     │   │  └─────────────────────┘   │
│                     │   │  ┌─────────────────────┐   │
│                     │   │  │   Tool Framework    │   │
│                     │   │  └─────────────────────┘   │
│                     │   │  ┌──────────┐ ┌─────────┐  │
│                     │   │  │AI Provid.│ │ Vector  │  │
│                     │   │  │          │ │  Store  │  │
│                     │   │  └──────────┘ └─────────┘  │
└─────────────────────┘   └─────────────────────────────┘
```

### 1. Complete Decoupling

El módulo de Intelligence debe ser completamente independiente del Core de BotPulse. El Core nunca debe conocer qué LLM, embedding provider, vector database, o framework de orchestration se está usando. La comunicación ocurre únicamente a través de contratos/interfaces. El módulo debe ser reemplazable sin modificar el resto de la aplicación.

**Rationale:** Evitar vendor lock-in y permitir que las capacidades de IA evolucionen independientemente del Core operacional. Cambiar de OpenAI a Anthropic, o reemplazar el framework de orchestration, no debe impactar `BotPulse.Core`.

### 2. Independent Deployment

El módulo debe estar diseñado para eventualmente desplegarse como un servicio independiente (ASP.NET service, Docker container, Kubernetes pod, cloud microservice).

**Rationale:** Separar los workloads de IA (intensivos en CPU/GPU) de los workloads operacionales permite escalar cada uno de forma independiente y facilita el uso de infraestructura especializada.

### 3. Independent Database

El módulo NO DEBE depender de la base de datos operativa de BotPulse. Posee su propia capa de persistencia: vector database, knowledge store, AI cache, prompt repository, conversation history.

**Rationale:** Aislar el ciclo de vida de datos de IA del ciclo de vida de datos operacionales. Diferentes requisitos de retención, backup, y compliance. Permite usar tecnologías especializadas sin afectar el operational DB.

### 4. Provider Pattern Everywhere

Todas las dependencias externas de IA deben estar abstraídas mediante el Provider Pattern. External AI services, vector databases, embedding models, y cualquier componente específico de vendor debe accederse únicamente a través de abstracciones.

**Rationale:** Preservar flexibilidad de cambiar proveedores sin modificar el Core. Soportar múltiples proveedores simultáneamente. Facilitar testing con mocks.

### 5. Intelligence Platform Instead of RAG

No modelar la arquitectura alrededor de una capacidad específica (RAG). La plataforma debe soportar múltiples capacidades de IA: RAG, semantic search, AI agents, prompt management, embeddings, tool calling, knowledge base, recommendations, predictive analysis, y capacidades futuras aún no definidas.

**Rationale:** RAG es solo una técnica. La arquitectura debe permanecer válida si RAG es reemplazado. Agregar nuevas capacidades no debe requerir cambios arquitectónicos.

### 6. Agent Framework

La plataforma debe exponer un Agent Framework genérico capaz de host specialized agents. Los agents comparten infraestructura común: state management, memory context (short-term y long-term), tool invocation, human-in-the-loop approval workflows.

**Rationale:** Evitar reimplementar lógica de orchestration para cada nuevo tipo de agent. Permite agregar nuevos agents sin modificar la infraestructura base.

### 7. Tool Framework

Los agents deben interactuar con la plataforma a través de un Tool Framework. Las herramientas son abstracciones de operaciones que la IA puede invocar. Los agents nunca deben acceder directamente a infraestructura interna.

**Rationale:** Control y auditoría de operaciones ejecutadas por IA. Permite implementar rate limiting, validación, y sandboxing por herramienta.

### 8. Vector Storage Abstraction

El storage vectorial debe estar abstraído. La arquitectura no debe asumir PostgreSQL+pgvector, Pinecone, Qdrant, o cualquier implementación específica.

**Rationale:** Los vector databases son tecnología emergente. Preservar flexibilidad para cambiar de implementación sin modificar la lógica de agents o RAG pipelines.

### 9. Provider-Agnostic AI Capabilities

Las capacidades de IA deben definirse de forma agnóstica a proveedores RPA. No usar lenguaje específico de UiPath, Blue Prism, etc. en la arquitectura.

**Rationale:** BotPulse es multi-vendor RPA. Las capacidades de IA deben funcionar con cualquier proveedor.

### 10. Event-Driven Integration

La Intelligence Platform debe reaccionar a domain events generados por `BotPulse.Core` siempre que sea posible, en lugar de participar directamente en workflows operacionales.

**Rationale:** Minimiza acoplamiento. Un agent de diagnóstico puede suscribirse a `JobFailedEvent` sin que el Core conozca su existencia. Agregar o remover capacidades de IA no requiere modificar el Core.

### 11. Security & Authorization Enforcement

The Intelligence Platform must enforce its own security boundaries when processing customer operational data.

AI capabilities must respect:
- **Tenant Isolation:** Knowledge and embeddings from one organization must never be accessible to another
- **Authorization Context:** AI operations must inherit and respect the authorization context from ADR-017
- **Data Retention Policies:** AI-processed data must follow the same retention rules as operational data
- **Sensitive Data Handling:** PII and sensitive information must be handled according to data classification policies
- **Audit Requirements:** All AI operations that access or process customer data must be auditable

**The Intelligence Platform must never bypass BotPulse authorization rules defined in ADR-017.**

**Rationale:** AI systems have unique security risks (prompt injection, data leakage through embeddings, cross-tenant contamination). The Intelligence Platform must not become a backdoor that bypasses normal authorization checks.

### 12. Human-in-the-Loop for Autonomous Actions

AI-generated operational changes must require explicit human approval before execution.

The Intelligence Platform may recommend, simulate, or prepare corrective actions, but autonomous execution must be controlled through approval workflows.

**Rationale:** Prevents unsafe automation behavior and provides governance for AI-driven operations. Self-healing agents can suggest fixes, but humans must review and approve before applying changes to production.

---

## Relationship with ADR-017

ADR-016 and ADR-017 evolve together as a combined security and intelligence architecture initiative.

**ADR-017 provides:**
- Identity management and user lifecycle
- Authentication abstraction (`IAuthenticationProvider`)
- Authorization engine (RBAC, `IAuthorizationService`)
- `AuthorizationContext` (userId, organizationId, permissions, tenantId)
- Security boundaries and audit infrastructure

**ADR-016 consumes:**
- `AuthorizationContext` to scope AI operations per user
- Permission validation before accessing operational data
- Tenant context for vector store isolation
- Audit infrastructure to log AI operations

Neither module creates duplicated security concepts. The Intelligence Platform never implements its own user or permission model.

---

## Implementation Constraint

Aunque la arquitectura debe permanecer framework-agnostic, la implementación inicial (MVP) puede aprovechar **Microsoft Semantic Kernel** como framework de orchestration detrás de las abstracciones.

**Semantic Kernel permanece un detalle de implementación** oculto detrás de las interfaces de la plataforma. Reemplazar Semantic Kernel debe requerir solo cambios en la capa de implementación, no en los contratos.

---

## Alternatives Considered

### Alternative 1: RAG-only implementation con LLM directo

Implementar solo RAG directamente acoplado al Core sin abstracciones.

**Rechazado:** No escalable. Cada nueva capacidad de IA requeriría modificar la arquitectura. Cambiar de LLM provider impactaría el Core.

### Alternative 2: Solución SaaS (OpenAI Assistants API, LangChain Cloud)

**Rechazado:** Requiere enviar datos sensibles de clientes a terceros. Costos elevados y lock-in vendor. No permite modelos locales (Ollama) para clientes con restricciones de privacidad.

### Alternative 3: Arquitectura monolítica con framework de IA como dependency pública

Introducir `Microsoft.SemanticKernel` como dependencia directa de `BotPulse.Core`.

**Rechazado:** Viola decoupling. Cambiar el framework requeriría modificar el Core. No permite deployment independiente.

### Alternative 4: Múltiples bases de datos especializadas (una por capacidad)

**Rechazado:** Over-engineering para el MVP. Complejidad operacional excesiva. La decisión correcta es: módulo separado + boundary separado + deployment independiente futuro — sin necesidad de 15 servicios desde el día uno.

---

## Consequences

### Positivas

- **Extensibilidad:** Agregar nuevas capacidades de IA requiere solo implementar interfaces, no modificar el Core.
- **Provider Independence:** Cambiar de LLM provider, vector database, o framework requiere solo cambiar la implementación.
- **Deployment Flexibility:** Desplegable como servicio independiente o como parte del monolito.
- **Data Isolation:** Gestiona su propio ciclo de vida de datos, incluyendo retención y compliance.
- **Scalability:** Cada componente puede escalar horizontalmente de forma independiente.
- **Security by Design:** Authorization enforcement, tenant isolation, y audit built-in desde el inicio.
- **Future-proof:** Arquitectura válida para capacidades futuras sin reescritura.

### Negativas

- **Complejidad Inicial:** Más compleja que una implementación RAG directa acoplada al Core.
- **Overhead de Abstracción:** Capas adicionales de indirección (mitigado con caching agresivo).
- **Development Time:** Más tiempo inicial (mitigado con MVP incremental).
- **Operational Complexity:** Múltiples componentes (mitigado con Docker/Kubernetes).

### Mitigaciones

- MVP con PostgreSQL + pgvector simplifica despliegue inicial.
- Caching agresivo de embeddings y LLM results.
- Monitoring y logging integrado desde el inicio.
- Incremental: RAG → Agents → Predictions, validando en cada paso.

---

## Dependencies

- **ADR-017 (RBAC contracts/interfaces):** La Intelligence Platform depende de las interfaces de autorización de ADR-017, no de su implementación completa. Puede integrarse en cuanto `IAuthorizationService` y `IAuthorizationContextAccessor` estén definidos.
- **PostgreSQL 15+** con extensión `pgvector` habilitada (MVP).
- **BotPulse.Core** debe exponer domain events (`JobFailedEvent`, `AlertTriggeredEvent`, etc.).
- **Fase 5 (Multi-Tenant):** Requiere particionado por `organization_id` en todas las tablas de Intelligence Platform.

---

## References

- Clean Architecture by Robert C. Martin
- Domain-Driven Design by Eric Evans
- Microsoft Semantic Kernel Documentation
- RAG Design Patterns
- Vector Database Comparison Studies
- Agent Patterns (Semantic Kernel Agents)

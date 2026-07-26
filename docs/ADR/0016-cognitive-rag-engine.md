# ADR-016: BotPulse Cognitive & RAG Engine

## Status
Proposed (Planned for Fase 5)

## Context
Las plataformas RPA generan grandes volúmenes de datos operacionales: logs de ejecución, trazas de errores, selectores que fallan, y resoluciones manuales por operadores. Este conocimiento permanece disperso y no se reutiliza para diagnosticar nuevos fallos.

Los operadores RPA dedican tiempo significativo a:
1. Diagnosticar la causa raíz de un fallo manualmente leyendo logs extensos.
2. Buscar si el mismo error ya ocurrió antes y cómo se resolvió.
3. Actualizar selectores rotos cuando las aplicaciones target cambian su UI.
4. Identificar degradaciones de rendimiento antes de que impacten las colas.

La combinación de Retrieval-Augmented Generation (RAG) con LLMs ofrece una solución para convertir este conocimiento operacional acumulado en diagnósticos automáticos contextualizados, reduciendo el MTTR (Mean Time to Resolve) significativamente.

## Decision
Se introduce un módulo `BotPulse.Cognitive` que implementa:

1. **Base de conocimiento vectorial** usando PostgreSQL + pgvector para almacenar embeddings de errores históricos, resoluciones y patrones de fallo.
2. **Pipeline RAG** que ante un job fallido recupera contexto histórico relevante y lo inyecta en el prompt del LLM.
3. **Panel de diagnóstico asistido** en la UI que muestra causa raíz, impacto y pasos de resolución en lenguaje natural.
4. **Búsqueda NL-to-Query** que permite a operadores consultar la plataforma en lenguaje natural.
5. **Agente de auto-reparación** (Self-Healing) capaz de sugerir parches de selectores previa aprobación humana.
6. **Feedback loop** que vectoriza nuevas resoluciones validadas por operadores para nutrir el RAG continuamente.
7. **Motor de predicción de anomalías** usando análisis estadístico de series temporales para alertas proactivas.

El módulo sigue el Provider Pattern de BotPulse:
- `IAIService` — abstracción para el LLM (OpenAI, Anthropic, Ollama/local)
- `IVectorSearchRepository` — abstracción para búsqueda vectorial (pgvector, Pinecone, Qdrant)
- `IEmbeddingProvider` — abstracción para generación de embeddings

## Alternatives Considered

**LLM directo sin RAG (solo prompt engineering)**
Más simple pero propenso a alucinaciones. El LLM no tendría contexto del historial específico de la empresa. Descartado por calidad de respuestas insuficiente.

**Base de conocimiento basada en keywords (Elasticsearch)**
Búsqueda léxica en lugar de semántica. Funciona para matches exactos pero falla con errores similares expresados diferente. Descartado por limitación semántica.

**Solución externa SaaS (Datadog AI, Splunk ITSI)**
Herramientas existentes de AIOps. Requieren enviar todos los datos a un tercero, no entienden el dominio RPA, y tienen costos elevados. Descartado por privacidad y costo.

## Consequences

**Positivas:**
- Reduce MTTR drásticamente al proveer diagnósticos contextualizados automáticamente.
- El conocimiento operacional se acumula y mejora con cada resolución (flywheel effect).
- Self-Healing reduce el mantenimiento correctivo más costoso en RPA (selectores rotos).
- Búsqueda NL democratiza el acceso a datos operacionales para usuarios no técnicos.
- El desacoplamiento de LLM providers permite usar modelos locales para clientes con restricciones de datos.

**Negativas:**
- Complejidad significativa: pgvector, pipeline de embeddings, integración LLM, agente autónomo.
- Costos de API de LLM por token (mitigado con truncamiento y caché de respuestas).
- Riesgo de alucinaciones del LLM (mitigado con RAG + validación humana en Self-Healing).
- Requiere Fase 4 (Multi-Tenant) completada para el aislamiento vectorial por cliente.
- Latencia adicional en diagnósticos (mitigado con procesamiento asíncrono post-fallo).

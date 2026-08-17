-- Intelligence Platform: independent persistence for vector knowledge (ADR-016 §3)
-- This schema lives alongside the operational DB in the MVP but is logically
-- owned by BotPulse.Intelligence, not BotPulse.Core.

CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS ai_knowledge (
    id              TEXT PRIMARY KEY,               -- deterministic id => idempotent indexing
    source_type     TEXT NOT NULL,
    content         TEXT NOT NULL,
    embedding       vector(1536),                    -- dimension matches configured embedding model
    metadata        JSONB NOT NULL DEFAULT '{}',
    organization_id UUID,                             -- nullable: multi-tenant ready (ADR-016 §11)
    created_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ai_knowledge_embedding
    ON ai_knowledge USING ivfflat (embedding vector_cosine_ops);

CREATE INDEX IF NOT EXISTS idx_ai_knowledge_org
    ON ai_knowledge (organization_id);

CREATE INDEX IF NOT EXISTS idx_ai_knowledge_source_type
    ON ai_knowledge (source_type);

CREATE TABLE IF NOT EXISTS ai_diagnosis_feedback (
    id             BIGSERIAL PRIMARY KEY,
    job_id         TEXT NOT NULL,
    diagnosis      JSONB NOT NULL,
    validated      BOOLEAN,                          -- null = pending, true/false = operator signal
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ai_diagnosis_feedback_job
    ON ai_diagnosis_feedback (job_id);

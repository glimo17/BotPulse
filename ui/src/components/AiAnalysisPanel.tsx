import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Sparkles, ThumbsUp, ThumbsDown, Loader2 } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import api from '@/lib/api'

interface Diagnosis {
  rootCause: string
  impact: string
  resolutionSteps: string[]
  confidence: number
  referencedKnowledgeIds: string[]
}

interface Props {
  providerName: string
  externalJobId: string
}

function confidenceColor(confidence: number): string {
  if (confidence >= 0.7) return 'text-green-400'
  if (confidence >= 0.4) return 'text-amber-400'
  return 'text-red-400'
}

export function AiAnalysisPanel({ providerName, externalJobId }: Props) {
  const { t } = useTranslation()
  const { hasPermission } = useAuth()
  const [diagnosis, setDiagnosis] = useState<Diagnosis | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [feedback, setFeedback] = useState<'validated' | 'rejected' | null>(null)

  const canDiagnose = hasPermission('Intelligence.Diagnose')
  if (!canDiagnose) return null

  const runDiagnosis = async () => {
    setLoading(true)
    setError(null)
    setFeedback(null)
    try {
      const res = await api.post<Diagnosis>(
        `/intelligence/diagnose/${providerName}/${externalJobId}`)
      setDiagnosis(res.data)
    } catch (e: any) {
      setError(e.response?.data?.error ?? t('ai.error'))
    } finally {
      setLoading(false)
    }
  }

  const submitFeedback = async (validated: boolean) => {
    if (!diagnosis) return
    try {
      await api.post(`/intelligence/diagnose/${externalJobId}/feedback`, {
        validated,
        rootCause: diagnosis.rootCause,
        impact: diagnosis.impact,
        resolutionSteps: diagnosis.resolutionSteps,
        confidence: diagnosis.confidence,
      })
      setFeedback(validated ? 'validated' : 'rejected')
    } catch {
      setError(t('ai.error'))
    }
  }

  return (
    <div className="mt-3 pt-3 border-t border-[var(--color-border)]">
      <div className="flex items-center gap-1.5 mb-2">
        <Sparkles size={14} className="text-accent" />
        <span className="text-xs font-semibold text-[var(--color-text-primary)]">{t('ai.title')}</span>
      </div>

      {!diagnosis && !loading && (
        <button
          onClick={runDiagnosis}
          className="w-full flex items-center justify-center gap-1.5 py-1.5 text-xs bg-accent/20 text-accent hover:bg-accent/30 rounded border border-accent/30 transition-colors">
          <Sparkles size={12} />{t('ai.analyze')}
        </button>
      )}

      {loading && (
        <div className="flex items-center justify-center gap-2 py-3 text-xs text-[var(--color-text-muted)]">
          <Loader2 size={14} className="animate-spin" />{t('ai.analyzing')}
        </div>
      )}

      {error && (
        <p className="text-xs text-red-400 py-2">{error}</p>
      )}

      {diagnosis && !loading && (
        <div className="space-y-2.5 text-xs">
          <div className="flex items-center justify-between">
            <span className="text-[var(--color-text-muted)]">{t('ai.confidence')}</span>
            <span className={`font-mono font-medium ${confidenceColor(diagnosis.confidence)}`}>
              {Math.round(diagnosis.confidence * 100)}%
            </span>
          </div>

          <div>
            <p className="text-[var(--color-text-muted)] mb-0.5">{t('ai.rootCause')}</p>
            <p className="text-[var(--color-text-secondary)]">{diagnosis.rootCause}</p>
          </div>

          <div>
            <p className="text-[var(--color-text-muted)] mb-0.5">{t('ai.impact')}</p>
            <p className="text-[var(--color-text-secondary)]">{diagnosis.impact}</p>
          </div>

          {diagnosis.resolutionSteps.length > 0 && (
            <div>
              <p className="text-[var(--color-text-muted)] mb-0.5">{t('ai.resolutionSteps')}</p>
              <ol className="list-decimal list-inside space-y-0.5 text-[var(--color-text-secondary)]">
                {diagnosis.resolutionSteps.map((step, i) => <li key={i}>{step}</li>)}
              </ol>
            </div>
          )}

          {feedback === null ? (
            <div className="flex gap-2 pt-2 border-t border-[var(--color-border)]">
              <button
                onClick={() => submitFeedback(true)}
                className="flex-1 flex items-center justify-center gap-1.5 py-1.5 text-xs bg-green-500/15 text-green-400 hover:bg-green-500/25 rounded border border-green-500/30 transition-colors">
                <ThumbsUp size={12} />{t('ai.helpful')}
              </button>
              <button
                onClick={() => submitFeedback(false)}
                className="flex-1 flex items-center justify-center gap-1.5 py-1.5 text-xs bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-hover)] rounded border border-[var(--color-border)] transition-colors">
                <ThumbsDown size={12} />{t('ai.notHelpful')}
              </button>
            </div>
          ) : (
            <p className="text-xs text-[var(--color-text-muted)] pt-2 border-t border-[var(--color-border)]">
              {feedback === 'validated' ? t('ai.thanksValidated') : t('ai.thanksRejected')}
            </p>
          )}
        </div>
      )}
    </div>
  )
}

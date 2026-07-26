import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import api from '@/lib/api'
import { clsx } from 'clsx'

interface Queue { externalId: string; name: string; totalItems: number; processedItems: number; failedItems: number; pendingItems: number }

export default function Queues() {
  const { t } = useTranslation()
  const { data: queues = [], isLoading } = useQuery<Queue[]>({
    queryKey: ['queues'],
    queryFn: () => api.get('/queues').then(r => r.data),
    staleTime: 120_000,
  })

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-xl font-bold text-[var(--color-text-primary)]">{t('queues.title')}</h1>
        <p className="text-sm text-[var(--color-text-muted)] mt-0.5">{queues.length} colas</p>
      </div>
      {isLoading ? (
        <div className="card p-8 text-center text-[var(--color-text-muted)] text-sm">{t('common.loadingQueues')}</div>
      ) : queues.length === 0 ? (
        <div className="card p-8 text-center text-[var(--color-text-muted)] text-sm">{t('common.noData')}</div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          {queues.map(q => {
            const pct = q.totalItems > 0 ? Math.round(q.processedItems / q.totalItems * 100) : 0
            const backlogPct = q.totalItems > 0 ? q.pendingItems / q.totalItems : 0
            const barColor = backlogPct > 0.95 ? 'bg-error' : backlogPct > 0.8 ? 'bg-warning' : 'bg-success'
            return (
              <div key={q.externalId} className="card p-4 space-y-3">
                <div className="flex items-start justify-between">
                  <p className="text-sm font-medium text-[var(--color-text-primary)]">{q.name}</p>
                  <span className={clsx('text-xs font-bold px-2 py-0.5 rounded-full',
                    q.pendingItems > 2000 ? 'bg-error/20 text-error' :
                    q.pendingItems > 500  ? 'bg-warning/20 text-warning' : 'bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)]')}>
                    {q.pendingItems} pend.
                  </span>
                </div>
                <div>
                  <div className="flex justify-between text-xs text-[var(--color-text-muted)] mb-1">
                    <span>{pct}% procesado</span>
                    <span>{q.processedItems}/{q.totalItems}</span>
                  </div>
                  <div className="h-1.5 bg-[var(--color-bg-hover)] rounded-full overflow-hidden">
                    <div className={clsx('h-full rounded-full transition-all', barColor)} style={{ width: `${pct}%` }} />
                  </div>
                </div>
                <div className="flex gap-4 text-xs">
                  <span className="text-success">{q.processedItems} ok</span>
                  <span className="text-error">{q.failedItems} fail</span>
                  <span className="text-[var(--color-text-muted)]">{q.totalItems} total</span>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

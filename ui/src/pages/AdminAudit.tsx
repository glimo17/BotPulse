import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Search, RefreshCw } from 'lucide-react'
import api from '@/lib/api'

interface AuditRecord {
  userId: string
  userName: string
  action: string
  resourceType: string
  resourceId: string | null
  outcome: string
  ipAddress: string | null
  correlationId: string
  detailsJson: string | null
  timestampUtc: string | null
}

export default function AdminAudit() {
  const { t } = useTranslation()
  const [userId, setUserId] = useState('')
  const [action, setAction] = useState('')
  const [applied, setApplied] = useState<{ userId: string; action: string }>({ userId: '', action: '' })

  const { data: records = [], isLoading, isError, refetch, isFetching } = useQuery<AuditRecord[]>({
    queryKey: ['auditLog', applied],
    queryFn: () => {
      const params = new URLSearchParams()
      if (applied.userId) params.set('userId', applied.userId)
      if (applied.action) params.set('action', applied.action)
      params.set('top', '200')
      return api.get<AuditRecord[]>(`/audit?${params.toString()}`).then(r => r.data)
    },
    staleTime: 15_000,
  })

  const applyFilters = () => setApplied({ userId: userId.trim(), action: action.trim() })

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-2">
        <div className="relative flex-1">
          <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-[var(--color-text-muted)]" />
          <input
            className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-bg-secondary)] pl-9 pr-4 py-2 text-sm text-[var(--color-text-primary)] focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]"
            placeholder={t('admin.filterByUser')}
            value={userId}
            onChange={e => setUserId(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && applyFilters()}
          />
        </div>
        <input
          className="flex-1 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg-secondary)] px-4 py-2 text-sm text-[var(--color-text-primary)] focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]"
          placeholder={t('admin.filterByAction')}
          value={action}
          onChange={e => setAction(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && applyFilters()}
        />
        <button
          onClick={applyFilters}
          className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white hover:opacity-90 transition-opacity"
        >
          {t('common.search')}
        </button>
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 rounded-lg border border-[var(--color-border)] px-3 py-2 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-hover)] transition-colors"
          title={t('common.refresh')}
        >
          <RefreshCw size={15} className={isFetching ? 'animate-spin' : ''} />
        </button>
      </div>

      {/* Table */}
      <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-secondary)] overflow-hidden">
        {isLoading && <p className="p-4 text-sm text-[var(--color-text-muted)]">{t('common.loading')}</p>}
        {isError && <p className="p-4 text-sm text-red-400">{t('common.error')}</p>}
        {!isLoading && !isError && records.length === 0 && (
          <p className="p-4 text-sm text-[var(--color-text-muted)]">{t('admin.noAuditRecords')}</p>
        )}
        {!isLoading && !isError && records.length > 0 && (
          <table className="w-full text-sm">
            <thead className="border-b border-[var(--color-border)] bg-[var(--color-bg)]">
              <tr>
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-muted)]">{t('admin.timestamp')}</th>
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-muted)]">{t('auth.username')}</th>
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-muted)]">{t('admin.action')}</th>
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-muted)]">{t('admin.resource')}</th>
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-muted)]">{t('admin.outcome')}</th>
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-muted)]">IP</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--color-border)]">
              {records.map((r, i) => (
                <tr key={`${r.correlationId}-${i}`} className="hover:bg-[var(--color-bg-hover)] transition-colors">
                  <td className="px-4 py-2.5 text-xs text-[var(--color-text-muted)] whitespace-nowrap">
                    {r.timestampUtc ? new Date(r.timestampUtc).toLocaleString() : '—'}
                  </td>
                  <td className="px-4 py-2.5 text-[var(--color-text-primary)]">{r.userName}</td>
                  <td className="px-4 py-2.5">
                    <span className="rounded-md bg-[var(--color-accent)]/10 px-2 py-0.5 text-xs text-[var(--color-accent)] font-mono">
                      {r.action}
                    </span>
                  </td>
                  <td className="px-4 py-2.5 text-xs text-[var(--color-text-secondary)]">
                    {r.resourceType}{r.resourceId ? ` · ${r.resourceId.substring(0, 8)}` : ''}
                  </td>
                  <td className="px-4 py-2.5">
                    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${
                      r.outcome === 'Success'
                        ? 'bg-green-500/10 text-green-400'
                        : 'bg-red-500/10 text-red-400'
                    }`}>
                      {r.outcome}
                    </span>
                  </td>
                  <td className="px-4 py-2.5 text-xs text-[var(--color-text-muted)] font-mono">{r.ipAddress ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}

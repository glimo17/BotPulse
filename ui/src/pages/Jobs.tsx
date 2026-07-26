import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { RefreshCw, X, ChevronRight, Square, XCircle, RotateCcw } from 'lucide-react'
import { StatusBadge } from '@/components/StatusBadge'
import { CopyableId } from '@/components/CopyableId'
import { useDensity } from '@/contexts/DensityContext'
import { useAuth } from '@/contexts/AuthContext'
import api from '@/lib/api'
import { clsx } from 'clsx'

interface Job {
  externalJobId: string
  providerName: string
  processExternalId: string
  robotExternalId: string
  machineExternalId?: string
  status: { value: string; isTerminal: boolean; isActive: boolean }
  startTimeUtc: string
  endTimeUtc?: string
  duration?: string
  errorType?: string
  errorMessage?: string
}

interface JobsResponse { items: Job[]; total: number; page: number; pageSize: number }

const STATUS_OPTS = ['All', 'Pending', 'Running', 'Success', 'Failed', 'Stopped', 'Cancelled']

function formatDuration(iso?: string): string {
  if (!iso) return '—'
  const m = iso.match(/PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+(?:\.\d+)?)S)?/)
  if (!m) return iso
  const h = parseInt(m[1]||'0'), mn = parseInt(m[2]||'0'), s = Math.floor(parseFloat(m[3]||'0'))
  if (h > 0) return `${h}h ${mn}m`
  if (mn > 0) return `${mn}m ${s}s`
  return `${s}s`
}

function fmtTime(iso: string): string {
  return new Date(iso).toLocaleString('es', { month:'short', day:'2-digit', hour:'2-digit', minute:'2-digit' })
}

export default function Jobs() {
  const { t } = useTranslation()
  const { cellPadding } = useDensity()
  const { user } = useAuth()
  const qc = useQueryClient()
  const isOperator = user?.roles?.some(r => r === 'Operator' || r === 'Administrator') ?? false

  const [status, setStatus] = useState('All')
  const [page, setPage] = useState(1)
  const [selected, setSelected] = useState<Job | null>(null)

  const params = new URLSearchParams({ page: String(page), pageSize: '50', sortDesc: 'true' })
  if (status !== 'All') params.set('status', status)

  const { data, isLoading } = useQuery<JobsResponse>({
    queryKey: ['jobs', status, page],
    queryFn: () => api.get(`/jobs?${params}`).then(r => r.data),
    staleTime: 30_000,
  })

  const stop   = useMutation({ mutationFn: (id: string) => api.post(`/jobs/${id}/stop?provider=UiPath`),   onSuccess: () => { void qc.invalidateQueries({ queryKey: ['jobs'] }); setSelected(null) } })
  const cancel = useMutation({ mutationFn: (id: string) => api.post(`/jobs/${id}/cancel?provider=UiPath`), onSuccess: () => { void qc.invalidateQueries({ queryKey: ['jobs'] }); setSelected(null) } })
  const retry  = useMutation({ mutationFn: (id: string) => api.post(`/jobs/${id}/retry?provider=UiPath`),  onSuccess: () => void qc.invalidateQueries({ queryKey: ['jobs'] }) })

  const jobs  = data?.items ?? []
  const total = data?.total ?? 0
  const pages = Math.ceil(total / 50)

  return (
    <div className="flex gap-4 h-full">
      <div className="flex-1 min-w-0 space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-xl font-bold text-gray-100">{t('jobs.title')}</h1>
            <p className="text-sm text-gray-500 mt-0.5">{total} jobs</p>
          </div>
          <button onClick={() => void qc.invalidateQueries({ queryKey: ['jobs'] })}
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs text-gray-400 hover:text-gray-200 bg-gray-800 hover:bg-gray-700 rounded-md border border-gray-700 transition-colors">
            <RefreshCw size={13} />{t('common.refresh')}
          </button>
        </div>

        <div className="flex gap-1.5 flex-wrap">
          {STATUS_OPTS.map(s => (
            <button key={s} onClick={() => { setStatus(s); setPage(1) }}
              className={clsx('px-2.5 py-1 rounded-full text-xs border transition-colors',
                status === s ? 'bg-accent/20 text-accent border-accent/40' : 'text-gray-400 border-gray-700 hover:text-gray-200 hover:bg-gray-800')}>
              {s}
            </button>
          ))}
        </div>

        <div className="card overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-700 bg-gray-900">
                  <th className={clsx('text-left text-xs text-gray-400 font-medium', cellPadding)}>ID</th>
                  <th className={clsx('text-left text-xs text-gray-400 font-medium hidden md:table-cell', cellPadding)}>Proceso</th>
                  <th className={clsx('text-left text-xs text-gray-400 font-medium hidden lg:table-cell', cellPadding)}>Robot</th>
                  <th className={clsx('text-left text-xs text-gray-400 font-medium', cellPadding)}>Estado</th>
                  <th className={clsx('text-left text-xs text-gray-400 font-medium hidden md:table-cell', cellPadding)}>Inicio</th>
                  <th className={clsx('text-left text-xs text-gray-400 font-medium', cellPadding)}>Duración</th>
                  {isOperator && <th className={clsx('text-left text-xs text-gray-400 font-medium', cellPadding)}>Acciones</th>}
                </tr>
              </thead>
              <tbody>
                {isLoading ? (
                <tr><td colSpan={7} className="px-4 py-8 text-center text-gray-500">{t('common.loadingJobs')}</td></tr>
                ) : jobs.length === 0 ? (
                  <tr><td colSpan={7} className="px-4 py-8 text-center text-gray-500">{t('common.noData')}</td></tr>
                ) : jobs.map(job => (
                  <tr key={job.externalJobId} onClick={() => setSelected(job)}
                    className={clsx('border-b border-gray-800 hover:bg-gray-800/60 cursor-pointer transition-colors',
                      selected?.externalJobId === job.externalJobId ? 'bg-gray-800/80' : '')}>
                    <td className={cellPadding}><CopyableId id={job.externalJobId} maxLength={10} /></td>
                    <td className={clsx('text-gray-300 hidden md:table-cell max-w-[160px] truncate', cellPadding)}>{job.processExternalId}</td>
                    <td className={clsx('hidden lg:table-cell', cellPadding)}><CopyableId id={job.robotExternalId} maxLength={8} /></td>
                    <td className={cellPadding}><StatusBadge status={job.status?.value ?? '—'} showDot /></td>
                    <td className={clsx('text-gray-400 hidden md:table-cell text-xs', cellPadding)}>{fmtTime(job.startTimeUtc)}</td>
                    <td className={clsx('text-gray-400 font-mono text-xs', cellPadding)}>{formatDuration(job.duration)}</td>
                    {isOperator && (
                      <td className={cellPadding} onClick={e => e.stopPropagation()}>
                        <div className="flex gap-1">
                          {job.status?.value === 'Running' && (
                            <button onClick={() => stop.mutate(job.externalJobId)} title="Stop"
                              className="p-1 text-error hover:bg-error/20 rounded transition-colors"><Square size={12} /></button>
                          )}
                          {job.status?.value === 'Pending' && (
                            <button onClick={() => cancel.mutate(job.externalJobId)} title="Cancel"
                              className="p-1 text-gray-400 hover:bg-gray-700 rounded transition-colors"><XCircle size={12} /></button>
                          )}
                          {(job.status?.value === 'Failed' || job.status?.value === 'Stopped') && (
                            <button onClick={() => retry.mutate(job.externalJobId)} title="Retry"
                              className="p-1 text-accent hover:bg-accent/20 rounded transition-colors"><RotateCcw size={12} /></button>
                          )}
                        </div>
                      </td>
                    )}
                    <td className={cellPadding}><ChevronRight size={14} className="text-gray-600" /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {pages > 1 && (
            <div className="flex items-center justify-between px-4 py-2 border-t border-gray-700">
              <span className="text-xs text-gray-500">Página {page} de {pages}</span>
              <div className="flex gap-1">
                <button onClick={() => setPage(p => Math.max(1, p-1))} disabled={page === 1}
                  className="px-2 py-1 text-xs text-gray-400 hover:text-gray-200 disabled:opacity-40 bg-gray-800 rounded border border-gray-700">Ant</button>
                <button onClick={() => setPage(p => Math.min(pages, p+1))} disabled={page === pages}
                  className="px-2 py-1 text-xs text-gray-400 hover:text-gray-200 disabled:opacity-40 bg-gray-800 rounded border border-gray-700">Sig</button>
              </div>
            </div>
          )}
        </div>
      </div>

      {selected && (
        <div className="w-80 shrink-0">
          <div className="card">
            <div className="flex items-center justify-between px-4 py-3 border-b border-gray-700">
              <span className="text-sm font-medium text-gray-200">Detalle del Job</span>
              <button onClick={() => setSelected(null)} className="text-gray-500 hover:text-gray-300"><X size={15} /></button>
            </div>
            <div className="p-4 space-y-3 text-sm">
              <div><StatusBadge status={selected.status?.value ?? '—'} showDot /></div>
              <div className="flex justify-between"><span className="text-gray-500">Job ID</span><CopyableId id={selected.externalJobId} maxLength={16} /></div>
              <div className="flex justify-between"><span className="text-gray-500">Proceso</span><span className="text-gray-300 text-xs text-right max-w-[150px] truncate">{selected.processExternalId}</span></div>
              <div className="flex justify-between"><span className="text-gray-500">Robot</span><CopyableId id={selected.robotExternalId} maxLength={12} /></div>
              <div className="flex justify-between"><span className="text-gray-500">Inicio</span><span className="text-gray-300 text-xs">{fmtTime(selected.startTimeUtc)}</span></div>
              <div className="flex justify-between"><span className="text-gray-500">Duración</span><span className="text-gray-300 font-mono">{formatDuration(selected.duration)}</span></div>
              {selected.errorMessage && (
                <div className="mt-3 p-3 bg-error/10 border border-error/20 rounded-md">
                  <p className="text-xs text-error font-medium mb-1">{selected.errorType || 'Error'}</p>
                  <p className="text-xs text-gray-400">{selected.errorMessage}</p>
                </div>
              )}
              {isOperator && (
                <div className="flex gap-2 mt-4 pt-3 border-t border-gray-700">
                  {selected.status?.value === 'Running' && (
                    <button onClick={() => stop.mutate(selected.externalJobId)}
                      className="flex-1 flex items-center justify-center gap-1.5 py-1.5 text-xs bg-error/20 text-error hover:bg-error/30 rounded border border-error/30 transition-colors">
                      <Square size={12} />Stop
                    </button>
                  )}
                  {selected.status?.value === 'Pending' && (
                    <button onClick={() => cancel.mutate(selected.externalJobId)}
                      className="flex-1 flex items-center justify-center gap-1.5 py-1.5 text-xs bg-gray-700 text-gray-300 hover:bg-gray-600 rounded border border-gray-600 transition-colors">
                      <XCircle size={12} />Cancel
                    </button>
                  )}
                  {(selected.status?.value === 'Failed' || selected.status?.value === 'Stopped') && (
                    <button onClick={() => retry.mutate(selected.externalJobId)}
                      className="flex-1 flex items-center justify-center gap-1.5 py-1.5 text-xs bg-accent/20 text-accent hover:bg-accent/30 rounded border border-accent/30 transition-colors">
                      <RotateCcw size={12} />Retry
                    </button>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

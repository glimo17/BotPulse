import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { RefreshCw, X, ChevronRight } from 'lucide-react'
import { StatusBadge } from '@/components/StatusBadge'
import { CopyableId } from '@/components/CopyableId'
import { useDensity } from '@/contexts/DensityContext'
import api from '@/lib/api'
import { clsx } from 'clsx'

interface Robot {
  externalId: string; name: string; status: string
  machineExternalId?: string; licenseType?: string; lastHeartbeatUtc: string
}

const STATUS_FILTERS = ['All', 'Online', 'Offline', 'Idle', 'Busy']

function timeAgo(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime()
  const m = Math.floor(diff / 60000)
  if (m < 1) return 'ahora'
  if (m < 60) return `hace ${m}m`
  if (m < 1440) return `hace ${Math.floor(m / 60)}h`
  return `hace ${Math.floor(m / 1440)}d`
}

export default function Robots() {
  const { t } = useTranslation()
  const { cellPadding } = useDensity()
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('All')
  const [selected, setSelected] = useState<Robot | null>(null)
  const [forceRefresh, setForceRefresh] = useState(0)

  const { data: robots = [], isLoading, refetch } = useQuery<Robot[]>({
    queryKey: ['robots', forceRefresh],
    queryFn: () => api.get('/robots').then(r => r.data),
    staleTime: 60_000,
  })

  const filtered = robots.filter(r => {
    const matchSearch = r.name.toLowerCase().includes(search.toLowerCase())
    const matchStatus = statusFilter === 'All' || r.status === statusFilter
    return matchSearch && matchStatus
  })

  return (
    <div className="flex gap-4 h-full">
      {/* Main content */}
      <div className="flex-1 min-w-0 space-y-4">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-xl font-bold text-[var(--color-text-primary)]">{t('robots.title')}</h1>
            <p className="text-sm text-[var(--color-text-muted)] mt-0.5">{filtered.length} de {robots.length} robots</p>
          </div>
          <button
            onClick={() => { setForceRefresh(v => v + 1); void refetch() }}
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] bg-[var(--color-bg-hover)] hover:bg-[var(--color-bg-hover)] rounded-md transition-colors border border-[var(--color-border)]"
          >
            <RefreshCw size={13} />
            {t('common.forceRefresh')}
          </button>
        </div>

        {/* Filters */}
        <div className="flex items-center gap-3 flex-wrap">
          {/* Search */}
          <input
            type="text"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder={t('common.search')}
            className="bg-[var(--color-bg-hover)] border border-[var(--color-border)] rounded-md px-3 py-1.5 text-sm text-[var(--color-text-primary)] placeholder-[var(--color-text-muted)] focus:outline-none focus:border-accent w-48 transition-colors"
          />
          {/* Status chips */}
          <div className="flex gap-1.5 flex-wrap">
            {STATUS_FILTERS.map(s => (
              <button
                key={s}
                onClick={() => setStatusFilter(s)}
                className={clsx(
                  'px-2.5 py-1 rounded-full text-xs transition-colors border',
                  statusFilter === s
                    ? 'bg-accent/20 text-accent border-accent/40'
                    : 'text-[var(--color-text-secondary)] border-[var(--color-border)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)]'
                )}
              >
                {s}
              </button>
            ))}
          </div>
        </div>

        {/* Table */}
        <div className="card overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-[var(--color-border)] bg-[var(--color-bg-secondary)]">
                  <th className={clsx('text-left text-xs text-[var(--color-text-secondary)] font-medium', cellPadding)}>Nombre</th>
                  <th className={clsx('text-left text-xs text-[var(--color-text-secondary)] font-medium', cellPadding)}>Estado</th>
                  <th className={clsx('text-left text-xs text-[var(--color-text-secondary)] font-medium hidden md:table-cell', cellPadding)}>Máquina</th>
                  <th className={clsx('text-left text-xs text-[var(--color-text-secondary)] font-medium hidden lg:table-cell', cellPadding)}>Licencia</th>
                  <th className={clsx('text-left text-xs text-[var(--color-text-secondary)] font-medium hidden md:table-cell', cellPadding)}>Último pulso</th>
                  <th className={clsx('text-left text-xs text-[var(--color-text-secondary)] font-medium', cellPadding)}>ID</th>
                  <th className={clsx('w-8', cellPadding)}></th>
                </tr>
              </thead>
              <tbody>
                {isLoading ? (
                  <tr><td colSpan={7} className="px-4 py-8 text-center text-[var(--color-text-muted)] text-sm">{t('common.loadingRobots')}</td></tr>
                ) : filtered.length === 0 ? (
                  <tr><td colSpan={7} className="px-4 py-8 text-center text-[var(--color-text-muted)] text-sm">{t('common.noData')}</td></tr>
                ) : (
                  filtered.map(robot => (
                    <tr
                      key={robot.externalId}
                      onClick={() => setSelected(robot)}
                      className={clsx(
                        'border-b border-[var(--color-border)] hover:bg-[var(--color-bg-hover)] cursor-pointer transition-colors',
                        selected?.externalId === robot.externalId ? 'bg-[var(--color-bg-hover)]' : ''
                      )}
                    >
                      <td className={clsx('text-[var(--color-text-primary)] font-medium', cellPadding)}>{robot.name}</td>
                      <td className={cellPadding}><StatusBadge status={robot.status} showDot /></td>
                      <td className={clsx('text-[var(--color-text-secondary)] hidden md:table-cell', cellPadding)}>
                        {robot.machineExternalId ? <CopyableId id={robot.machineExternalId} maxLength={8} /> : '—'}
                      </td>
                      <td className={clsx('text-[var(--color-text-secondary)] hidden lg:table-cell', cellPadding)}>{robot.licenseType || '—'}</td>
                      <td className={clsx('text-[var(--color-text-secondary)] hidden md:table-cell', cellPadding)}>{timeAgo(robot.lastHeartbeatUtc)}</td>
                      <td className={cellPadding}><CopyableId id={robot.externalId} /></td>
                      <td className={cellPadding}>
                        <ChevronRight size={14} className="text-gray-600" />
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* Detail Drawer */}
      {selected && (
        <div className="w-80 shrink-0">
          <div className="card h-full">
            <div className="flex items-center justify-between px-4 py-3 border-b border-[var(--color-border)]">
              <span className="text-sm font-medium text-[var(--color-text-primary)]">Detalle del Robot</span>
              <button onClick={() => setSelected(null)} className="text-[var(--color-text-muted)] hover:text-[var(--color-text-secondary)] transition-colors">
                <X size={15} />
              </button>
            </div>
            <div className="p-4 space-y-4">
              <div>
                <p className="text-lg font-bold text-[var(--color-text-primary)]">{selected.name}</p>
                <StatusBadge status={selected.status} showDot className="mt-1.5" />
              </div>
              <div className="space-y-3 text-sm">
                <div className="flex justify-between">
                  <span className="text-[var(--color-text-muted)]">External ID</span>
                  <CopyableId id={selected.externalId} maxLength={16} />
                </div>
                <div className="flex justify-between">
                  <span className="text-[var(--color-text-muted)]">Máquina</span>
                  <span className="text-[var(--color-text-secondary)]">
                    {selected.machineExternalId
                      ? <CopyableId id={selected.machineExternalId} maxLength={12} />
                      : '—'}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-[var(--color-text-muted)]">Licencia</span>
                  <span className="text-[var(--color-text-secondary)]">{selected.licenseType || '—'}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-[var(--color-text-muted)]">Último pulso</span>
                  <span className="text-[var(--color-text-secondary)]">{timeAgo(selected.lastHeartbeatUtc)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-[var(--color-text-muted)]">Timestamp</span>
                  <span className="text-[var(--color-text-secondary)] text-xs font-mono">
                    {new Date(selected.lastHeartbeatUtc).toLocaleString()}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

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
            <h1 className="text-xl font-bold text-gray-100">{t('robots.title')}</h1>
            <p className="text-sm text-gray-500 mt-0.5">{filtered.length} de {robots.length} robots</p>
          </div>
          <button
            onClick={() => { setForceRefresh(v => v + 1); void refetch() }}
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs text-gray-400 hover:text-gray-200 bg-gray-800 hover:bg-gray-700 rounded-md transition-colors border border-gray-700"
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
            className="bg-gray-800 border border-gray-700 rounded-md px-3 py-1.5 text-sm text-gray-200 placeholder-gray-500 focus:outline-none focus:border-accent w-48 transition-colors"
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
                    : 'text-gray-400 border-gray-700 hover:text-gray-200 hover:bg-gray-800'
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
                <tr className="border-b border-gray-700 bg-gray-900">
                  <th className={clsx('text-left text-xs text-gray-400 font-medium', cellPadding)}>Nombre</th>
                  <th className={clsx('text-left text-xs text-gray-400 font-medium', cellPadding)}>Estado</th>
                  <th className={clsx('text-left text-xs text-gray-400 font-medium hidden md:table-cell', cellPadding)}>Máquina</th>
                  <th className={clsx('text-left text-xs text-gray-400 font-medium hidden lg:table-cell', cellPadding)}>Licencia</th>
                  <th className={clsx('text-left text-xs text-gray-400 font-medium hidden md:table-cell', cellPadding)}>Último pulso</th>
                  <th className={clsx('text-left text-xs text-gray-400 font-medium', cellPadding)}>ID</th>
                  <th className={clsx('w-8', cellPadding)}></th>
                </tr>
              </thead>
              <tbody>
                {isLoading ? (
                  <tr><td colSpan={7} className="px-4 py-8 text-center text-gray-500 text-sm">{t('common.loadingRobots')}</td></tr>
                ) : filtered.length === 0 ? (
                  <tr><td colSpan={7} className="px-4 py-8 text-center text-gray-500 text-sm">{t('common.noData')}</td></tr>
                ) : (
                  filtered.map(robot => (
                    <tr
                      key={robot.externalId}
                      onClick={() => setSelected(robot)}
                      className={clsx(
                        'border-b border-gray-800 hover:bg-gray-800/60 cursor-pointer transition-colors',
                        selected?.externalId === robot.externalId ? 'bg-gray-800/80' : ''
                      )}
                    >
                      <td className={clsx('text-gray-200 font-medium', cellPadding)}>{robot.name}</td>
                      <td className={cellPadding}><StatusBadge status={robot.status} showDot /></td>
                      <td className={clsx('text-gray-400 hidden md:table-cell', cellPadding)}>
                        {robot.machineExternalId ? <CopyableId id={robot.machineExternalId} maxLength={8} /> : '—'}
                      </td>
                      <td className={clsx('text-gray-400 hidden lg:table-cell', cellPadding)}>{robot.licenseType || '—'}</td>
                      <td className={clsx('text-gray-400 hidden md:table-cell', cellPadding)}>{timeAgo(robot.lastHeartbeatUtc)}</td>
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
            <div className="flex items-center justify-between px-4 py-3 border-b border-gray-700">
              <span className="text-sm font-medium text-gray-200">Detalle del Robot</span>
              <button onClick={() => setSelected(null)} className="text-gray-500 hover:text-gray-300 transition-colors">
                <X size={15} />
              </button>
            </div>
            <div className="p-4 space-y-4">
              <div>
                <p className="text-lg font-bold text-gray-100">{selected.name}</p>
                <StatusBadge status={selected.status} showDot className="mt-1.5" />
              </div>
              <div className="space-y-3 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-500">External ID</span>
                  <CopyableId id={selected.externalId} maxLength={16} />
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Máquina</span>
                  <span className="text-gray-300">
                    {selected.machineExternalId
                      ? <CopyableId id={selected.machineExternalId} maxLength={12} />
                      : '—'}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Licencia</span>
                  <span className="text-gray-300">{selected.licenseType || '—'}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Último pulso</span>
                  <span className="text-gray-300">{timeAgo(selected.lastHeartbeatUtc)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Timestamp</span>
                  <span className="text-gray-300 text-xs font-mono">
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

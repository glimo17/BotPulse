import React from 'react'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Bot, Briefcase, ListOrdered, Bell, RefreshCw, Pause, Play } from 'lucide-react'
import { StatusBadge } from '@/components/StatusBadge'
import { CopyableId } from '@/components/CopyableId'
import { useAutoRefresh } from '@/hooks/useAutoRefresh'
import api from '@/lib/api'

interface Robot { externalId: string; name: string; status: string; machineExternalId?: string; lastHeartbeatUtc: string }
interface Job { externalJobId: string; processExternalId: string; robotExternalId: string; status: { value: string }; startTimeUtc: string; duration?: string }

function formatDuration(iso?: string): string {
  if (!iso) return '—'
  const match = iso.match(/PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+(?:\.\d+)?)S)?/)
  if (!match) return iso
  const h = parseInt(match[1] || '0'), m = parseInt(match[2] || '0'), s = Math.floor(parseFloat(match[3] || '0'))
  if (h > 0) return `${h}h ${m}m`
  if (m > 0) return `${m}m ${s}s`
  return `${s}s`
}

function timeAgo(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime()
  const m = Math.floor(diff / 60000)
  if (m < 1) return 'ahora'
  if (m < 60) return `${m}m`
  return `${Math.floor(m / 60)}h`
}

export default function Dashboard() {
  const { t } = useTranslation()

  const { data: robots, refetch: refetchRobots } = useQuery<Robot[]>({
    queryKey: ['robots'],
    queryFn: () => api.get('/robots').then(r => r.data),
    staleTime: 60_000,
  })

  const { data: jobsData, refetch: refetchJobs } = useQuery<{ items: Job[]; total: number }>({
    queryKey: ['jobs-dashboard'],
    queryFn: () => api.get('/jobs?pageSize=20&sortDesc=true').then(r => r.data),
    staleTime: 30_000,
  })

  const refetchAll = () => { void refetchRobots(); void refetchJobs() }
  const { paused, countdown, pause, resume, forceRefresh } = useAutoRefresh(30, refetchAll)

  const onlineRobots = robots?.filter(r => r.status === 'Online' || r.status === 'Idle' || r.status === 'Busy').length ?? 0
  const offlineRobots = robots?.filter(r => r.status === 'Offline').length ?? 0
  const jobs = jobsData?.items ?? []
  const successJobs = jobs.filter(j => j.status?.value === 'Success').length
  const failedJobs  = jobs.filter(j => j.status?.value === 'Failed').length

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-xl font-bold text-gray-100">{t('nav.dashboard')}</h1>
          <p className="text-sm text-gray-500 mt-0.5">Plataforma de operaciones RPA</p>
        </div>
        {/* Auto-refresh controls */}
        <div className="flex items-center gap-2 text-xs text-gray-500">
          <span>{paused ? 'Pausado' : t('common.refreshIn', { seconds: countdown })}</span>
          <button onClick={paused ? resume : pause} className="p-1 hover:text-gray-300 transition-colors">
            {paused ? <Play size={13} /> : <Pause size={13} />}
          </button>
          <button onClick={forceRefresh} className="p-1 hover:text-gray-300 transition-colors">
            <RefreshCw size={13} />
          </button>
        </div>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="card p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-8 h-8 rounded-lg bg-accent/20 flex items-center justify-center">
              <Bot size={16} className="text-accent" />
            </div>
            <span className="text-xs text-gray-400 uppercase tracking-wide">Robots</span>
          </div>
          <p className="text-2xl font-bold text-gray-100">{robots?.length ?? '—'}</p>
          <div className="flex gap-3 mt-2 text-xs">
            <span className="text-success">{onlineRobots} online</span>
            <span className="text-error">{offlineRobots} offline</span>
          </div>
        </div>

        <div className="card p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-8 h-8 rounded-lg bg-success/20 flex items-center justify-center">
              <Briefcase size={16} className="text-success" />
            </div>
            <span className="text-xs text-gray-400 uppercase tracking-wide">Jobs</span>
          </div>
          <p className="text-2xl font-bold text-gray-100">{jobs.length}</p>
          <div className="flex gap-3 mt-2 text-xs">
            <span className="text-success">{successJobs} ok</span>
            <span className="text-error">{failedJobs} fail</span>
          </div>
        </div>

        <div className="card p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-8 h-8 rounded-lg bg-warning/20 flex items-center justify-center">
              <ListOrdered size={16} className="text-warning" />
            </div>
            <span className="text-xs text-gray-400 uppercase tracking-wide">Colas</span>
          </div>
          <p className="text-2xl font-bold text-gray-100">—</p>
          <p className="text-xs text-gray-500 mt-2">Pendientes en cola</p>
        </div>

        <div className="card p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-8 h-8 rounded-lg bg-error/20 flex items-center justify-center">
              <Bell size={16} className="text-error" />
            </div>
            <span className="text-xs text-gray-400 uppercase tracking-wide">Alertas</span>
          </div>
          <p className="text-2xl font-bold text-gray-100">—</p>
          <p className="text-xs text-gray-500 mt-2">Activas sin atender</p>
        </div>
      </div>

      {/* Robots status + Recent jobs */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Robots mini-grid */}
        <div className="card">
          <div className="px-4 py-3 border-b border-gray-700 flex items-center justify-between">
            <span className="text-sm font-medium text-gray-200">Estado de Robots</span>
            <span className="text-xs text-gray-500">{robots?.length ?? 0} total</span>
          </div>
          <div className="p-3 grid grid-cols-1 gap-2 max-h-64 overflow-y-auto">
            {!robots ? (
              <p className="text-xs text-gray-500 p-2">Cargando robots...</p>
            ) : robots.length === 0 ? (
              <p className="text-xs text-gray-500 p-2">No hay robots en este folder</p>
            ) : (
              robots.map(robot => (
                <div key={robot.externalId} className="flex items-center justify-between px-3 py-2 bg-gray-800 rounded-md">
                  <div className="flex items-center gap-2 min-w-0">
                    <StatusBadge status={robot.status} showDot />
                    <span className="text-xs text-gray-200 truncate">{robot.name}</span>
                  </div>
                  <span className="text-xs text-gray-500 shrink-0 ml-2">{timeAgo(robot.lastHeartbeatUtc)}</span>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Recent jobs table */}
        <div className="card">
          <div className="px-4 py-3 border-b border-gray-700 flex items-center justify-between">
            <span className="text-sm font-medium text-gray-200">Jobs Recientes</span>
            <span className="text-xs text-gray-500">{jobsData?.total ?? 0} total</span>
          </div>
          <div className="overflow-x-auto max-h-64 overflow-y-auto">
            <table className="w-full text-xs">
              <thead className="sticky top-0 bg-gray-900">
                <tr className="border-b border-gray-700">
                  <th className="px-3 py-2 text-left text-gray-400 font-medium">ID</th>
                  <th className="px-3 py-2 text-left text-gray-400 font-medium">Proceso</th>
                  <th className="px-3 py-2 text-left text-gray-400 font-medium">Estado</th>
                  <th className="px-3 py-2 text-left text-gray-400 font-medium">Duración</th>
                </tr>
              </thead>
              <tbody>
                {jobs.length === 0 ? (
                  <tr><td colSpan={4} className="px-3 py-4 text-center text-gray-500">Sin jobs recientes</td></tr>
                ) : (
                  jobs.slice(0, 15).map(job => (
                    <tr key={job.externalJobId} className="border-b border-gray-800 hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-2"><CopyableId id={job.externalJobId} maxLength={10} /></td>
                      <td className="px-3 py-2 text-gray-300 truncate max-w-[140px]">{job.processExternalId}</td>
                      <td className="px-3 py-2"><StatusBadge status={job.status?.value ?? '—'} /></td>
                      <td className="px-3 py-2 text-gray-400 font-mono">{formatDuration(job.duration)}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  )
}

import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Activity, Briefcase, Server, Bell, Clock, Bot, ListOrdered, Timer, RefreshCw, Pause, Play } from 'lucide-react'
import { StatusBadge } from '@/components/StatusBadge'
import { CopyableId } from '@/components/CopyableId'
import { KpiCard } from '@/components/KpiCard'
import { useAutoRefresh } from '@/hooks/useAutoRefresh'
import {
  calculateSuccessRate,
  calculateFleetAvailability,
  calculateRobotUtilization,
  calculateQueueBacklog,
  calculateMtta,
  calculateAvgCycleTime,
  calculateExceptionBreakdown,
  formatMtta,
  formatAvgCycleTime,
  getPercentageColor,
} from '@/lib/kpiCalculations'
import type { Robot, Job, Queue, Alert } from '@/lib/kpiCalculations'
import api from '@/lib/api'

interface JobsPage { items: Job[]; total: number }

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

  // 4 parallel queries
  const { data: robots, refetch: refetchRobots } = useQuery<Robot[]>({
    queryKey: ['robots'],
    queryFn: () => api.get('/robots').then(r => r.data),
    staleTime: 60_000,
  })

  const { data: jobsData, refetch: refetchJobs } = useQuery<JobsPage>({
    queryKey: ['jobs-dashboard'],
    queryFn: () => api.get('/jobs?pageSize=50&sortDesc=true').then(r => r.data),
    staleTime: 30_000,
  })

  const { data: queues, refetch: refetchQueues } = useQuery<Queue[]>({
    queryKey: ['queues-dashboard'],
    queryFn: () => api.get('/queues').then(r => r.data),
    staleTime: 60_000,
  })

  const { data: alerts, refetch: refetchAlerts } = useQuery<Alert[]>({
    queryKey: ['alerts-dashboard'],
    queryFn: () => api.get('/alerts').then(r => r.data),
    staleTime: 30_000,
  })

  const refetchAll = () => {
    void refetchRobots()
    void refetchJobs()
    void refetchQueues()
    void refetchAlerts()
  }
  const { paused, countdown, pause, resume, forceRefresh } = useAutoRefresh(30, refetchAll)

  // Derived data
  const jobs = jobsData?.items ?? []
  const robotList = robots ?? []
  const queueList = queues ?? []
  const alertList = alerts ?? []

  // KPI calculations
  const successRate = calculateSuccessRate(jobs)
  const successRateColor = getPercentageColor(successRate)

  const successCount = jobs.filter(j => j.status?.value === 'Success').length
  const failedCount = jobs.filter(j => j.status?.value === 'Failed').length
  const stoppedCount = jobs.filter(j => ['Stopped', 'Cancelled'].includes(j.status?.value)).length

  const avgCycleTime = calculateAvgCycleTime(jobs)
  const avgCycleTimeLabel = formatAvgCycleTime(avgCycleTime)

  const utilization = calculateRobotUtilization(robotList)

  const fleetAvail = calculateFleetAvailability(robotList)
  const fleetAvailColor = getPercentageColor(fleetAvail)

  const queueBacklog = calculateQueueBacklog(queueList)

  const excBreakdown = calculateExceptionBreakdown(jobs)

  const mttaMinutes = calculateMtta(alertList)
  const mttaLabel = formatMtta(mttaMinutes)

  const criticalAlerts = alertList.filter(a => a.severity === 'Critical' && !a.acknowledged).length
  const unacknowledgedAll = alertList.filter(a => !a.acknowledged).length

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-xl font-bold text-gray-100">{t('nav.dashboard')}</h1>
          <p className="text-sm text-gray-500 mt-0.5">{t('dashboard.subtitle')}</p>
        </div>
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

      {/* KPI Row 1 */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiCard
          label={t('kpi.successRate')}
          value={successRate !== null ? `${successRate}%` : null}
          subtitle={t('kpi.successOf', { count: successCount, total: jobs.length })}
          icon={Activity}
          iconColor="text-success"
          valueColor={successRateColor}
          href="/jobs"
        />
        <KpiCard
          label={t('kpi.jobsExecuted')}
          value={jobs.length || null}
          subtitle={t('kpi.okFailStop', { ok: successCount, fail: failedCount, stop: stoppedCount })}
          icon={Briefcase}
          iconColor="text-accent"
          href="/jobs"
        />
        <KpiCard
          label={t('kpi.fleetAvailability')}
          value={fleetAvail !== null ? `${fleetAvail}%` : null}
          subtitle={t('kpi.offlineOf', { offline: robotList.filter(r => r.status === 'Offline').length, total: robotList.length })}
          icon={Server}
          iconColor="text-success"
          valueColor={fleetAvailColor}
          href="/robots"
        />
        <KpiCard
          label={t('kpi.criticalAlerts')}
          value={criticalAlerts}
          subtitle={t('kpi.unacknowledged', { count: unacknowledgedAll })}
          icon={Bell}
          iconColor="text-error"
          valueColor={criticalAlerts > 0 ? 'text-error' : 'text-gray-100'}
          pulse={true}
          href="/alerts"
        />
      </div>

      {/* KPI Row 2 */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiCard
          label={t('kpi.avgCycleTime')}
          value={avgCycleTimeLabel}
          subtitle={t('kpi.avgExecDuration')}
          icon={Clock}
          iconColor="text-accent"
          href="/jobs"
        />
        <KpiCard
          label={t('kpi.robotUtilization')}
          value={utilization.rate !== null ? `${utilization.rate}%` : null}
          subtitle={t('kpi.busyOf', { busy: utilization.busyCount, total: utilization.totalCount })}
          icon={Bot}
          iconColor="text-warning"
          href="/robots"
        />
        <KpiCard
          label={t('kpi.queueBacklog')}
          value={queueBacklog}
          subtitle={t('kpi.activeQueues', { count: queueList.length })}
          icon={ListOrdered}
          iconColor="text-warning"
          href="/queues"
        />
        <KpiCard
          label={t('kpi.mtta')}
          value={mttaLabel}
          subtitle={excBreakdown.total > 0 ? t('kpi.exceptionBreakdown', { business: excBreakdown.businessExceptions, system: excBreakdown.systemExceptions }) : t('kpi.noAcknowledgedAlerts')}
          icon={Timer}
          iconColor="text-accent"
          href="/alerts"
        />
      </div>

      {/* Robots status + Recent jobs */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Robots mini-grid */}
        <div className="card">
          <div className="px-4 py-3 border-b border-gray-700 flex items-center justify-between">
            <span className="text-sm font-medium text-gray-200">{t('dashboard.robotsStatus')}</span>
            <span className="text-xs text-gray-500">{robotList.length} total</span>
          </div>
          <div className="p-3 grid grid-cols-1 gap-2 max-h-64 overflow-y-auto">
            {!robots ? (
              <p className="text-xs text-gray-500 p-2">{t('dashboard.loadingRobots')}</p>
            ) : robotList.length === 0 ? (
              <p className="text-xs text-gray-500 p-2">{t('dashboard.noRobots')}</p>
            ) : (
              robotList.map(robot => (
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
            <span className="text-sm font-medium text-gray-200">{t('dashboard.recentJobs')}</span>
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
                  <tr><td colSpan={4} className="px-3 py-4 text-center text-gray-500">{t('dashboard.noRecentJobs')}</td></tr>
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

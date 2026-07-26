// KPI Calculation Functions — Pure, no side effects, no dependencies
// Used by Dashboard.tsx and Metrics.tsx

export interface Robot {
  externalId: string
  name: string
  status: string
  lastHeartbeatUtc: string
  machineExternalId?: string
}

export interface Job {
  externalJobId: string
  processExternalId: string
  robotExternalId: string
  status: { value: string }
  startTimeUtc: string
  duration?: string
  errorType?: string
}

export interface Queue {
  name: string
  pendingItems: number
  processedItems: number
  failedItems: number
  totalItems: number
}

export interface Alert {
  id: string
  severity: string
  acknowledged: boolean
  raisedAtUtc: string
  acknowledgedAtUtc?: string | null
}

// Helper: parse ISO 8601 duration to seconds
function parseDurationToSeconds(iso: string): number {
  const match = iso.match(/PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+(?:\.\d+)?)S)?/)
  if (!match) return 0
  const h = parseInt(match[1] || '0')
  const m = parseInt(match[2] || '0')
  const s = parseFloat(match[3] || '0')
  return h * 3600 + m * 60 + s
}

// 1. Success Rate
export function calculateSuccessRate(jobs: Job[]): number | null {
  if (jobs.length === 0) return null
  const success = jobs.filter(j => j.status?.value === 'Success').length
  return Math.round((success / jobs.length) * 100)
}

// 2. Fleet Availability
export function calculateFleetAvailability(robots: Robot[]): number | null {
  if (robots.length === 0) return null
  const available = robots.filter(r =>
    ['Online', 'Idle', 'Busy'].includes(r.status)
  ).length
  return Math.round((available / robots.length) * 100)
}

// 3. Robot Utilization
export function calculateRobotUtilization(robots: Robot[]): {
  rate: number | null
  busyCount: number
  totalCount: number
} {
  const totalCount = robots.length
  const busyCount = robots.filter(r => r.status === 'Busy').length
  const rate = totalCount > 0 ? Math.round((busyCount / totalCount) * 100) : null
  return { rate, busyCount, totalCount }
}

// 4. Queue Backlog
export function calculateQueueBacklog(queues: Queue[]): number {
  return queues.reduce((sum, q) => sum + (q.pendingItems ?? 0), 0)
}

// 5. MTTA in minutes
export function calculateMtta(alerts: Alert[]): number | null {
  const acknowledged = alerts.filter(a => a.acknowledgedAtUtc != null)
  if (acknowledged.length === 0) return null
  const totalMs = acknowledged.reduce((sum, a) => {
    const raised = new Date(a.raisedAtUtc).getTime()
    const acked = new Date(a.acknowledgedAtUtc!).getTime()
    return sum + (acked - raised)
  }, 0)
  return Math.round(totalMs / 60000 / acknowledged.length)
}

// 6. Average Cycle Time in seconds
export function calculateAvgCycleTime(jobs: Job[]): number | null {
  const TERMINAL = ['Success', 'Failed', 'Stopped', 'Cancelled']
  const withDuration = jobs.filter(
    j => TERMINAL.includes(j.status?.value) && j.duration
  )
  if (withDuration.length === 0) return null
  const totalSeconds = withDuration.reduce((sum, j) => {
    return sum + parseDurationToSeconds(j.duration!)
  }, 0)
  return Math.round(totalSeconds / withDuration.length)
}

// 7. Exception Breakdown
export function calculateExceptionBreakdown(jobs: Job[]): {
  businessExceptions: number
  systemExceptions: number
  other: number
  total: number
} {
  const failed = jobs.filter(j => j.status?.value === 'Failed')
  const businessExceptions = failed.filter(j => j.errorType === 'BusinessException').length
  const systemExceptions = failed.filter(j => j.errorType === 'SystemException').length
  const other = failed.length - businessExceptions - systemExceptions
  return { businessExceptions, systemExceptions, other, total: failed.length }
}

// 8. Format MTTA
export function formatMtta(minutes: number | null): string {
  if (minutes === null) return '—'
  if (minutes < 60) return `${minutes}m`
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  return `${h}h ${m.toString().padStart(2, '0')}m`
}

// 9. Format Average Cycle Time
export function formatAvgCycleTime(seconds: number | null): string {
  if (seconds === null) return '—'
  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds % 3600) / 60)
  const s = Math.floor(seconds % 60)
  if (h > 0) return `${h}h ${m}m`
  if (m > 0) return `${m}m ${s}s`
  return `${s}s`
}

// 10. Color for percentage KPIs
export function getPercentageColor(value: number | null): string {
  if (value === null) return 'text-gray-400'
  if (value >= 90) return 'text-success'
  if (value >= 70) return 'text-warning'
  return 'text-error'
}

import { clsx } from 'clsx'

type Status = 'Online' | 'Offline' | 'Idle' | 'Busy' | 'Success' | 'Failed' | 'Running' | 'Pending' | 'Stopped' | 'Cancelled' | string

const STATUS_STYLES: Record<string, string> = {
  Online:    'badge-success',
  Success:   'badge-success',
  Offline:   'badge-error',
  Failed:    'badge-error',
  Idle:      'badge-idle',
  Stopped:   'badge-idle',
  Cancelled: 'badge-idle',
  Running:   'badge-running',
  Busy:      'badge-running',
  Pending:   'badge-warning',
  Warning:   'badge-warning',
  Critical:  'badge-error',
  Info:      'badge-info',
}

const STATUS_DOTS: Record<string, string> = {
  Online:  'bg-success animate-pulse-slow',
  Running: 'bg-running animate-pulse-slow',
  Busy:    'bg-running animate-pulse-slow',
  Offline: 'bg-error',
  Failed:  'bg-error',
}

interface Props {
  status: Status
  showDot?: boolean
  className?: string
}

export function StatusBadge({ status, showDot = false, className }: Props) {
  const style = STATUS_STYLES[status] || 'badge-idle'
  const dot = STATUS_DOTS[status]

  return (
    <span className={clsx(style, 'inline-flex items-center gap-1.5', className)}>
      {(showDot || dot) && (
        <span className={clsx('w-1.5 h-1.5 rounded-full', dot || 'bg-gray-400')} />
      )}
      {status}
    </span>
  )
}

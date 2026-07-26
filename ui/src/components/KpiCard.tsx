import { Link } from 'react-router-dom'
import { TrendingUp, TrendingDown } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

export interface KpiCardProps {
  label: string
  value: string | number | null
  subtitle: string
  icon: LucideIcon
  iconColor: string
  valueColor?: string
  href?: string
  pulse?: boolean
  trend?: {
    value: number
    label: string
  }
}

export function KpiCard({
  label, value, subtitle, icon: Icon, iconColor,
  valueColor, href, pulse, trend
}: KpiCardProps) {
  const displayValue = value === null || value === undefined ? '—' : value
  const showPulse = pulse && typeof value === 'number' && value > 0

  const iconBg = iconColor.replace('text-', 'bg-') + '/20'

  const content = (
    <div className="card p-5 h-full flex flex-col">
      <div className="flex items-center gap-3 mb-3">
        <div className={`w-8 h-8 rounded-lg ${iconBg} flex items-center justify-center shrink-0`}>
          <Icon size={16} className={iconColor} />
        </div>
        <span className="text-xs text-gray-400 uppercase tracking-wide font-medium">
          {label}
        </span>
        {showPulse && (
          <span className="relative flex h-2 w-2 ml-auto">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-error opacity-75" />
            <span className="relative inline-flex rounded-full h-2 w-2 bg-error" />
          </span>
        )}
      </div>
      <p className={`text-2xl font-bold ${valueColor ?? 'text-gray-100'}`}>
        {displayValue}
      </p>
      <p className="text-xs text-gray-500 mt-1">{subtitle}</p>
      {trend && (
        <div className="flex items-center gap-1 mt-2 text-xs text-gray-500">
          {trend.value > 0
            ? <TrendingUp size={12} className="text-success" />
            : <TrendingDown size={12} className="text-error" />
          }
          <span>{trend.label}</span>
        </div>
      )}
    </div>
  )

  if (href) {
    return (
      <Link to={href} className="block hover:ring-1 hover:ring-accent/40 rounded-lg transition-all">
        {content}
      </Link>
    )
  }
  return content
}

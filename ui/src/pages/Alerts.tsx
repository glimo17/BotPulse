import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { CheckCircle, AlertTriangle, Info, AlertOctagon } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import api from '@/lib/api'
import { clsx } from 'clsx'

interface Alert { id: string; severity: string; conditionDescription: string; affectedResourceType: string; affectedResourceId: string; raisedAtUtc: string; acknowledged: boolean; acknowledgedBy?: string }
interface AlertsResponse { items?: Alert[] }

const SEV_ICON: Record<string, React.ReactNode> = {
  Critical: <AlertOctagon size={14} className="text-error" />,
  Warning:  <AlertTriangle size={14} className="text-warning" />,
  Info:     <Info size={14} className="text-accent" />,
}
const SEV_STYLES: Record<string, string> = {
  Critical: 'border-l-2 border-error',
  Warning:  'border-l-2 border-warning',
  Info:     'border-l-2 border-accent',
}
const SEV_FILTERS = ['All', 'Critical', 'Warning', 'Info']

function timeAgo(iso: string) {
  const m = Math.floor((Date.now() - new Date(iso).getTime()) / 60000)
  if (m < 1) return 'ahora'
  if (m < 60) return `hace ${m}m`
  if (m < 1440) return `hace ${Math.floor(m/60)}h`
  return `hace ${Math.floor(m/1440)}d`
}

export default function Alerts() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const qc = useQueryClient()
  const isOperator = user?.roles?.some(r => r === 'Operator' || r === 'Administrator') ?? false
  const [sevFilter, setSevFilter] = useState('All')
  const [showAcked, setShowAcked] = useState(false)

  const { data, isLoading } = useQuery<Alert[] | AlertsResponse>({
    queryKey: ['alerts'],
    queryFn: () => api.get('/alerts').then(r => r.data),
    refetchInterval: 30_000,
  })
  const alerts: Alert[] = Array.isArray(data) ? data : (data as AlertsResponse)?.items ?? []

  const ack = useMutation({
    mutationFn: (id: string) => api.post(`/alerts/${id}/ack`),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['alerts'] }),
  })

  const filtered = alerts.filter(a => {
    if (sevFilter !== 'All' && a.severity !== sevFilter) return false
    if (!showAcked && a.acknowledged) return false
    return true
  })

  const activeCount = alerts.filter(a => !a.acknowledged).length

  return (
    <div className="space-y-4">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-xl font-bold text-gray-100">{t('alerts.title')}</h1>
          <p className="text-sm text-gray-500 mt-0.5">
            {activeCount > 0 ? <span className="text-error">{activeCount} activas sin atender</span> : 'Sin alertas activas'}
          </p>
        </div>
      </div>

      <div className="flex items-center gap-3 flex-wrap">
        <div className="flex gap-1.5">
          {SEV_FILTERS.map(s => (
            <button key={s} onClick={() => setSevFilter(s)}
              className={clsx('px-2.5 py-1 rounded-full text-xs border transition-colors',
                sevFilter === s ? 'bg-accent/20 text-accent border-accent/40' : 'text-gray-400 border-gray-700 hover:text-gray-200 hover:bg-gray-800')}>
              {s}
            </button>
          ))}
        </div>
        <label className="flex items-center gap-2 text-xs text-gray-400 cursor-pointer">
          <input type="checkbox" checked={showAcked} onChange={e => setShowAcked(e.target.checked)} className="rounded accent-accent" />
          Mostrar reconocidas
        </label>
      </div>

      {isLoading ? (
        <div className="card p-8 text-center text-gray-500 text-sm">{t('common.loadingAlerts')}</div>
      ) : filtered.length === 0 ? (
        <div className="card p-8 text-center text-gray-500 text-sm">
          {alerts.length === 0 ? 'Sin alertas registradas' : 'Sin alertas con los filtros aplicados'}
        </div>
      ) : (
        <div className="space-y-2">
          {filtered.map(alert => (
            <div key={alert.id}
              className={clsx('card p-4 flex items-start gap-3 transition-opacity',
                SEV_STYLES[alert.severity] || '',
                alert.acknowledged ? 'opacity-50' : '')}>
              <div className="mt-0.5 shrink-0">{SEV_ICON[alert.severity] ?? <Info size={14} className="text-gray-500" />}</div>
              <div className="flex-1 min-w-0">
                <p className="text-sm text-gray-200">{alert.conditionDescription}</p>
                <div className="flex items-center gap-3 mt-1 text-xs text-gray-500">
                  <span>{alert.affectedResourceType}/{alert.affectedResourceId}</span>
                  <span>·</span>
                  <span>{timeAgo(alert.raisedAtUtc)}</span>
                  {alert.acknowledged && <span className="text-success">· Reconocida por {alert.acknowledgedBy}</span>}
                </div>
              </div>
              {isOperator && !alert.acknowledged && (
                <button onClick={() => ack.mutate(alert.id)}
                  className="flex items-center gap-1.5 px-2.5 py-1 text-xs text-success border border-success/30 hover:bg-success/10 rounded-md transition-colors shrink-0">
                  <CheckCircle size={12} />
                  {t('alerts.acknowledge')}
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Server } from 'lucide-react'
import { StatusBadge } from '@/components/StatusBadge'
import { CopyableId } from '@/components/CopyableId'
import api from '@/lib/api'

interface Machine { externalId: string; name: string; status: string; connectedRobotCount: number; lastHeartbeatUtc: string }

function timeAgo(iso: string) {
  const m = Math.floor((Date.now() - new Date(iso).getTime()) / 60000)
  if (m < 1) return 'ahora'
  if (m < 60) return `hace ${m}m`
  return `hace ${Math.floor(m/60)}h`
}

export default function Machines() {
  const { t } = useTranslation()
  const { data: machines = [], isLoading } = useQuery<Machine[]>({
    queryKey: ['machines'],
    queryFn: () => api.get('/machines').then(r => r.data),
    staleTime: 120_000,
  })

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-xl font-bold text-gray-100">Máquinas</h1>
        <p className="text-sm text-gray-500 mt-0.5">{machines.length} máquinas</p>
      </div>
      {isLoading ? (
        <div className="card p-8 text-center text-gray-500 text-sm">{t('common.loadingMachines')}</div>
      ) : machines.length === 0 ? (
        <div className="card p-8 text-center text-gray-500 text-sm">{t('common.noData')}</div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {machines.map(m => (
            <div key={m.externalId} className="card p-4 space-y-3">
              <div className="flex items-start justify-between">
                <div className="flex items-center gap-2">
                  <div className="w-8 h-8 rounded-lg bg-gray-800 flex items-center justify-center">
                    <Server size={16} className="text-gray-400" />
                  </div>
                  <div>
                    <p className="text-sm font-medium text-gray-200">{m.name}</p>
                    <p className="text-xs text-gray-500">{m.connectedRobotCount} robots</p>
                  </div>
                </div>
                <StatusBadge status={m.status} showDot />
              </div>
              <div className="space-y-1.5 text-xs">
                <div className="flex justify-between">
                  <span className="text-gray-500">ID</span>
                  <CopyableId id={m.externalId} maxLength={14} />
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Último pulso</span>
                  <span className="text-gray-400">{timeAgo(m.lastHeartbeatUtc)}</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

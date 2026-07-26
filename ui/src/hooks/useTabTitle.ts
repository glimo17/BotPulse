import { useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import api from '@/lib/api'

interface Alert { id: string; acknowledged: boolean; severity: string }
interface AlertsResponse { items?: Alert[] }

export function useTabTitle() {
  const { data } = useQuery<Alert[] | AlertsResponse>({
    queryKey: ['alerts'],
    queryFn: () => api.get('/alerts').then(r => r.data),
    refetchInterval: 30_000,
    staleTime: 15_000,
  })

  const alerts: Alert[] = Array.isArray(data) ? data : (data as AlertsResponse)?.items ?? []
  const criticalCount = alerts.filter(a => !a.acknowledged && a.severity === 'Critical').length

  useEffect(() => {
    if (criticalCount > 0) {
      document.title = `⚠️ (${criticalCount}) BotPulse — Alerta Crítica`
    } else {
      document.title = 'BotPulse'
    }
    return () => { document.title = 'BotPulse' }
  }, [criticalCount])
}

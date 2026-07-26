import { useEffect, useCallback, useRef } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { getToken } from '@/lib/api'

export function useNotifications(
  onConnected?: (connected: boolean) => void,
  onEvent?: (eventType: string, data?: string) => void,
) {
  const qc = useQueryClient()
  const esRef = useRef<EventSource | null>(null)
  const retryDelay = useRef(1000)
  // Keep a stable ref to the callback to avoid re-triggering the effect
  const onConnectedRef = useRef(onConnected)
  onConnectedRef.current = onConnected
  const onEventRef = useRef(onEvent)
  onEventRef.current = onEvent

  const connect = useCallback(() => {
    // EventSource doesn't support Authorization headers, so pass the JWT as a query param
    const token = getToken()
    const url = token
      ? `/api/v1/notifications/stream?token=${encodeURIComponent(token)}`
      : `/api/v1/notifications/stream`
    const es = new EventSource(url)
    esRef.current = es

    es.onopen = () => {
      retryDelay.current = 1000
      onConnectedRef.current?.(true)
    }

    es.onerror = () => {
      onConnectedRef.current?.(false)
      es.close()
      esRef.current = null
      // Exponential backoff: 1s, 2s, 4s, 8s, 30s max
      const delay = Math.min(retryDelay.current, 30_000)
      retryDelay.current = Math.min(retryDelay.current * 2, 30_000)
      setTimeout(connect, delay)
    }

    es.addEventListener('job.state.changed', () => {
      void qc.invalidateQueries({ queryKey: ['jobs'] })
      void qc.invalidateQueries({ queryKey: ['jobs-dashboard'] })
    })

    es.addEventListener('alert.raised', (e: MessageEvent) => {
      void qc.invalidateQueries({ queryKey: ['alerts'] })
      onEventRef.current?.('alert.raised', e.data)
    })

    es.addEventListener('job.action.requested', () => {
      void qc.invalidateQueries({ queryKey: ['jobs'] })
    })
  }, [qc])

  useEffect(() => {
    connect()
    return () => {
      esRef.current?.close()
      esRef.current = null
    }
  }, [connect])
}

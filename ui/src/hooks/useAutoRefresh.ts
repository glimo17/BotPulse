import { useState, useEffect, useCallback, useRef } from 'react'

export function useAutoRefresh(intervalSeconds: number, onRefresh: () => void) {
  const [paused, setPaused] = useState(false)
  const [countdown, setCountdown] = useState(intervalSeconds)
  const callbackRef = useRef(onRefresh)
  callbackRef.current = onRefresh

  useEffect(() => {
    if (paused) return

    setCountdown(intervalSeconds)
    const interval = setInterval(() => {
      setCountdown(prev => {
        if (prev <= 1) {
          callbackRef.current()
          return intervalSeconds
        }
        return prev - 1
      })
    }, 1000)

    return () => clearInterval(interval)
  }, [paused, intervalSeconds])

  const pause  = useCallback(() => setPaused(true), [])
  const resume = useCallback(() => { setPaused(false); setCountdown(intervalSeconds) }, [intervalSeconds])
  const forceRefresh = useCallback(() => { callbackRef.current(); setCountdown(intervalSeconds) }, [intervalSeconds])

  return { paused, countdown, pause, resume, forceRefresh }
}

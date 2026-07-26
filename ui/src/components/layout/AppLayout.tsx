import { useState, useCallback, useEffect } from 'react'
import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'
import { Header } from './Header'
import { CommandPalette } from '@/components/CommandPalette'
import { useNotifications } from '@/hooks/useNotifications'
import { useTabTitle } from '@/hooks/useTabTitle'
import { ToastContainer, useToast } from '@/components/Toast'

export function AppLayout() {
  const [collapsed, setCollapsed] = useState(
    () => localStorage.getItem('botpulse-sidebar') === 'collapsed'
  )
  const [cmdOpen, setCmdOpen] = useState(false)
  const [sseConnected, setSseConnected] = useState(false)

  const { toasts, addToast, dismiss } = useToast()

  const handleSseStatus = useCallback((connected: boolean) => {
    setSseConnected(connected)
  }, [])

  const handleSseEvent = useCallback((eventType: string, data?: string) => {
    if (eventType === 'alert.raised') {
      try {
        const parsed = data ? JSON.parse(data) : null
        const severity = parsed?.severity ?? 'Critical'
        const description = parsed?.description ?? 'Nueva alerta generada'
        if (severity === 'Critical') {
          addToast(`🔴 ${description}`, 'Critical')
        }
      } catch { /* ignore */ }
    }
  }, [addToast])

  useNotifications(handleSseStatus, handleSseEvent)
  useTabTitle()

  const toggleSidebar = () => {
    setCollapsed(v => {
      const next = !v
      localStorage.setItem('botpulse-sidebar', next ? 'collapsed' : 'expanded')
      return next
    })
  }

  const openPalette = useCallback(() => setCmdOpen(true), [])
  const closePalette = useCallback(() => setCmdOpen(false), [])

  // Register global Ctrl+K shortcut
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault()
        setCmdOpen(v => !v)
      }
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [])

  return (
    <div className="flex h-screen bg-gray-950 overflow-hidden">
      <Sidebar collapsed={collapsed} onToggle={toggleSidebar} />
      <div className="flex flex-col flex-1 min-w-0">
        <Header onCommandPaletteOpen={openPalette} sseConnected={sseConnected} />
        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>
      <CommandPalette open={cmdOpen} onClose={closePalette} />
      <ToastContainer toasts={toasts} onDismiss={dismiss} />
    </div>
  )
}

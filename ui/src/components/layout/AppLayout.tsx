import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'
import { Header } from './Header'

interface Props {
  onCommandPaletteOpen: () => void
  sseConnected?: boolean
}

export function AppLayout({ onCommandPaletteOpen, sseConnected }: Props) {
  const [collapsed, setCollapsed] = useState(
    () => localStorage.getItem('botpulse-sidebar') === 'collapsed'
  )

  const toggleSidebar = () => {
    setCollapsed(v => {
      const next = !v
      localStorage.setItem('botpulse-sidebar', next ? 'collapsed' : 'expanded')
      return next
    })
  }

  return (
    <div className="flex h-screen bg-gray-950 overflow-hidden">
      <Sidebar collapsed={collapsed} onToggle={toggleSidebar} />
      <div className="flex flex-col flex-1 min-w-0">
        <Header onCommandPaletteOpen={onCommandPaletteOpen} sseConnected={sseConnected} />
        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

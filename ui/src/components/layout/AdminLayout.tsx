import { Outlet } from 'react-router-dom'
import { Header } from './Header'

interface Props {
  onCommandPaletteOpen: () => void
}

export function AdminLayout({ onCommandPaletteOpen }: Props) {
  return (
    <div className="flex h-screen bg-gray-950 overflow-hidden">
      <div className="flex flex-col flex-1 min-w-0">
        <Header onCommandPaletteOpen={onCommandPaletteOpen} />
        <main className="flex-1 overflow-y-auto p-6">
          <div className="max-w-7xl mx-auto">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}

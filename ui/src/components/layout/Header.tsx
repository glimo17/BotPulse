import { useLocation } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/contexts/AuthContext'
import { useDensity } from '@/contexts/DensityContext'
import { Search, AlignJustify, AlignLeft, LogOut, Globe, User } from 'lucide-react'
import { useState } from 'react'
import { clsx } from 'clsx'
import i18n from '@/i18n'
import { ThemeSelector } from '@/components/ThemeSelector'

const ROUTE_LABELS: Record<string, string> = {
  '/dashboard': 'nav.dashboard',
  '/robots':    'nav.robots',
  '/machines':  'nav.machines',
  '/processes': 'nav.processes',
  '/jobs':      'nav.jobs',
  '/launcher':  'nav.launcher',
  '/queues':    'nav.queues',
  '/logs':      'nav.logs',
  '/metrics':   'nav.metrics',
  '/alerts':    'nav.alerts',
}

interface Props {
  onCommandPaletteOpen: () => void
  sseConnected?: boolean
}

export function Header({ onCommandPaletteOpen, sseConnected = false }: Props) {
  const { t } = useTranslation()
  const { user, logout } = useAuth()
  const { density, toggleDensity } = useDensity()
  const location = useLocation()
  const [userMenuOpen, setUserMenuOpen] = useState(false)
  const [lang, setLang] = useState(i18n.language)

  const breadcrumb = t(ROUTE_LABELS[location.pathname] || 'nav.dashboard')

  const toggleLang = () => {
    const next = lang === 'es' ? 'en' : 'es'
    i18n.changeLanguage(next)
    localStorage.setItem('botpulse-lang', next)
    setLang(next)
  }

  return (
    <header className="h-12 flex items-center justify-between px-4 bg-[var(--color-bg-secondary)] border-b border-[var(--color-border)] shrink-0 sticky top-0 z-10">
      {/* Left: breadcrumb */}
      <div className="flex items-center gap-2 text-sm">
        <span className="text-[var(--color-text-muted)]">BotPulse</span>
        <span className="text-[var(--color-text-muted)]">/</span>
        <span className="text-[var(--color-text-primary)] font-medium">{breadcrumb}</span>
      </div>

      {/* Right: actions */}
      <div className="flex items-center gap-1">
        {/* Command palette button */}
        <button
          onClick={onCommandPaletteOpen}
          className="flex items-center gap-2 px-2.5 py-1.5 text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] rounded-md text-xs transition-colors border border-[var(--color-border)]"
        >
          <Search size={13} />
          <span className="hidden sm:block">Buscar</span>
          <kbd className="hidden sm:block text-[var(--color-text-muted)] text-[10px] font-mono bg-[var(--color-bg-hover)] px-1.5 py-0.5 rounded border border-[var(--color-border)]">⌃K</kbd>
        </button>

        {/* SSE indicator */}
        <div className="flex items-center gap-1.5 px-2 py-1.5" title={sseConnected ? 'Live updates active' : 'Disconnected'}>
          <span className={clsx(
            'w-1.5 h-1.5 rounded-full',
            sseConnected ? 'bg-success animate-pulse-slow' : 'bg-error'
          )} />
        </div>

        {/* Density toggle */}
        <button
          onClick={toggleDensity}
          className="p-1.5 text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] rounded-md transition-colors"
          title={density === 'comfortable' ? 'Switch to compact view' : 'Switch to comfortable view'}
        >
          {density === 'comfortable' ? <AlignJustify size={15} /> : <AlignLeft size={15} />}
        </button>

        {/* Language toggle */}
        <button
          onClick={toggleLang}
          className="flex items-center gap-1 px-2 py-1.5 text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] rounded-md text-xs transition-colors"
        >
          <Globe size={13} />
          <span>{lang.toUpperCase()}</span>
        </button>

        {/* Theme selector */}
        <ThemeSelector />

        {/* User menu */}
        <div className="relative">
          <button
            onClick={() => setUserMenuOpen(v => !v)}
            className="flex items-center gap-2 px-2 py-1.5 text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] rounded-md text-xs transition-colors"
          >
            <div className="w-6 h-6 rounded-full bg-[var(--color-accent)]/20 flex items-center justify-center">
              <User size={13} className="text-[var(--color-accent)]" />
            </div>
            <span className="hidden sm:block max-w-[80px] truncate">{user?.userName}</span>
          </button>

          {userMenuOpen && (
            <>
              <div className="fixed inset-0 z-40" onClick={() => setUserMenuOpen(false)} />
              <div className="absolute right-0 top-full mt-1 w-44 bg-[var(--color-bg-card)] border border-[var(--color-border)] rounded-lg shadow-xl z-50 py-1">
                <div className="px-3 py-2 border-b border-[var(--color-border)]">
                  <p className="text-xs text-[var(--color-text-primary)] font-medium truncate">{user?.userName}</p>
                  <p className="text-xs text-[var(--color-text-muted)] truncate">{user?.email}</p>
                </div>
                <button
                  onClick={() => { logout(); setUserMenuOpen(false) }}
                  className="w-full flex items-center gap-2 px-3 py-2 text-xs text-[var(--color-text-secondary)] hover:text-[var(--color-error)] hover:bg-[var(--color-bg-hover)] transition-colors"
                >
                  <LogOut size={13} />
                  {t('auth.logout')}
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </header>
  )
}

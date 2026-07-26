import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import {
  LayoutDashboard, Bot, Server, Workflow, Briefcase,
  ListOrdered, ScrollText, BarChart2, Bell, Search
} from 'lucide-react'
import api from '@/lib/api'
import { clsx } from 'clsx'

interface Robot { externalId: string; name: string; status: string }
interface Job   { externalJobId: string; processExternalId: string; status: { value: string } }

const PAGES = [
  { label: 'Dashboard',  path: '/dashboard', icon: LayoutDashboard },
  { label: 'Robots',     path: '/robots',    icon: Bot },
  { label: 'Máquinas',   path: '/machines',  icon: Server },
  { label: 'Procesos',   path: '/processes', icon: Workflow },
  { label: 'Jobs',       path: '/jobs',      icon: Briefcase },
  { label: 'Colas',      path: '/queues',    icon: ListOrdered },
  { label: 'Logs',       path: '/logs',      icon: ScrollText },
  { label: 'Métricas',   path: '/metrics',   icon: BarChart2 },
  { label: 'Alertas',    path: '/alerts',    icon: Bell },
]

interface Props { open: boolean; onClose: () => void }

export function CommandPalette({ open, onClose }: Props) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [query, setQuery] = useState('')

  const { data: robots = [] } = useQuery<Robot[]>({
    queryKey: ['robots'],
    queryFn: () => api.get('/robots').then(r => r.data),
    enabled: open,
    staleTime: 60_000,
  })

  const { data: jobsData } = useQuery<{ items: Job[] }>({
    queryKey: ['jobs-palette'],
    queryFn: () => api.get('/jobs?pageSize=10&sortDesc=true').then(r => r.data),
    enabled: open,
    staleTime: 30_000,
  })
  const recentJobs = jobsData?.items ?? []

  useEffect(() => {
    if (!open) { setQuery(''); return }
    const el = document.getElementById('cmd-input')
    el?.focus()
  }, [open])

  useEffect(() => {
    const handleKey = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') { e.preventDefault(); onClose() }
      if (e.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', handleKey)
    return () => window.removeEventListener('keydown', handleKey)
  }, [onClose])

  const q = query.toLowerCase()

  const filteredPages = q
    ? PAGES.filter(p => p.label.toLowerCase().includes(q))
    : PAGES

  const filteredRobots = robots.filter(r =>
    !q || r.name.toLowerCase().includes(q) || r.status.toLowerCase().includes(q)
  ).slice(0, 5)

  const filteredJobs = recentJobs.filter(j =>
    !q || j.externalJobId.toLowerCase().includes(q) || j.processExternalId.toLowerCase().includes(q)
  ).slice(0, 5)

  const handleSelect = (path: string) => {
    navigate(path)
    onClose()
  }

  if (!open) return null

  return (
    <>
      {/* Backdrop */}
      <div className="fixed inset-0 bg-black/60 z-50 backdrop-blur-sm" onClick={onClose} />

      {/* Palette */}
      <div className="fixed top-[20%] left-1/2 -translate-x-1/2 w-full max-w-xl z-50 px-4">
        <div className="bg-[var(--color-bg-secondary)] border border-[var(--color-border)] rounded-xl shadow-2xl overflow-hidden">
          {/* Search input */}
          <div className="flex items-center gap-3 px-4 py-3 border-b border-[var(--color-border)]">
            <Search size={16} className="text-[var(--color-text-muted)] shrink-0" />
            <input
              id="cmd-input"
              value={query}
              onChange={e => setQuery(e.target.value)}
              placeholder={t('common.search')}
              className="flex-1 bg-transparent text-[var(--color-text-primary)] placeholder-[var(--color-text-muted)] outline-none text-sm"
            />
            <kbd className="text-[var(--color-text-muted)] text-[10px] font-mono bg-[var(--color-bg-hover)] px-1.5 py-0.5 rounded border border-[var(--color-border)]">ESC</kbd>
          </div>

          {/* Results */}
          <div className="max-h-80 overflow-y-auto py-2">
            {/* Pages */}
            {filteredPages.length > 0 && (
              <div>
                <p className="px-4 py-1 text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider">Navegar a</p>
                {filteredPages.map(page => {
                  const Icon = page.icon
                  return (
                    <button key={page.path} onClick={() => handleSelect(page.path)}
                      className="w-full flex items-center gap-3 px-4 py-2 text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition-colors text-left">
                      <Icon size={14} className="text-[var(--color-text-muted)] shrink-0" />
                      {page.label}
                    </button>
                  )
                })}
              </div>
            )}

            {/* Robots */}
            {filteredRobots.length > 0 && (
              <div>
                <p className="px-4 py-1 text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider mt-1">Robots</p>
                {filteredRobots.map(robot => (
                  <button key={robot.externalId} onClick={() => handleSelect('/robots')}
                    className="w-full flex items-center gap-3 px-4 py-2 text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition-colors text-left">
                    <Bot size={14} className="text-[var(--color-text-muted)] shrink-0" />
                    <span className="flex-1">{robot.name}</span>
                    <span className={clsx('text-xs', robot.status === 'Online' ? 'text-success' : 'text-error')}>{robot.status}</span>
                  </button>
                ))}
              </div>
            )}

            {/* Recent jobs */}
            {filteredJobs.length > 0 && (
              <div>
                <p className="px-4 py-1 text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider mt-1">{t('common.recentJobs')}</p>
                {filteredJobs.map(job => (
                  <button key={job.externalJobId} onClick={() => handleSelect('/jobs')}
                    className="w-full flex items-center gap-3 px-4 py-2 text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition-colors text-left">
                    <Briefcase size={14} className="text-[var(--color-text-muted)] shrink-0" />
                    <span className="flex-1 font-mono text-xs">{job.externalJobId.substring(0, 14)}…</span>
                    <span className="text-xs text-[var(--color-text-muted)]">{job.status?.value ?? '—'}</span>
                  </button>
                ))}
              </div>
            )}

            {q && filteredPages.length === 0 && filteredRobots.length === 0 && filteredJobs.length === 0 && (
              <p className="px-4 py-6 text-center text-sm text-[var(--color-text-muted)]">Sin resultados para "{query}"</p>
            )}
          </div>
        </div>
      </div>
    </>
  )
}

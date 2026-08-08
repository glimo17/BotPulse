import { NavLink } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  LayoutDashboard, Bot, Server, Workflow, Briefcase,
  ListOrdered, ScrollText, BarChart2, Bell, ChevronLeft, ActivitySquare, Rocket,
  Settings2, Users, UserCog
} from 'lucide-react'
import { clsx } from 'clsx'
import { useAuth } from '@/contexts/AuthContext'
import { PermissionGate } from '@/components/PermissionGate'

const NAV_ITEMS = [
  { key: 'dashboard', icon: LayoutDashboard, path: '/dashboard' },
  { key: 'robots',    icon: Bot,             path: '/robots'    },
  { key: 'machines',  icon: Server,          path: '/machines'  },
  { key: 'processes', icon: Workflow,        path: '/processes' },
  { key: 'jobs',      icon: Briefcase,       path: '/jobs'      },
  { key: 'launcher',  icon: Rocket,          path: '/launcher'  },
  { key: 'queues',    icon: ListOrdered,     path: '/queues'    },
  { key: 'logs',      icon: ScrollText,      path: '/logs'      },
  { key: 'metrics',   icon: BarChart2,       path: '/metrics'   },
  { key: 'alerts',    icon: Bell,            path: '/alerts'    },
]

const ADMIN_NAV_GROUP = {
  parent: { key: 'admin.dashboard', icon: LayoutDashboard, path: '/admin' },
  children: [
    { key: 'admin.users',    icon: UserCog,  path: '/admin/users',    permission: 'Users.View'    },
    { key: 'admin.roles',    icon: Users,    path: '/admin/roles',    permission: 'Roles.View'    },
    { key: 'admin.settings', icon: Settings2, path: '/admin/settings', permission: 'Settings.View' },
  ],
}

interface Props {
  collapsed: boolean
  onToggle: () => void
}

export function Sidebar({ collapsed, onToggle }: Props) {
  const { t } = useTranslation()
  const { isAdmin } = useAuth()

  return (
    <aside className={clsx(
      'flex flex-col bg-[var(--color-bg-secondary)] border-r border-[var(--color-border)] h-screen sticky top-0 transition-all duration-200 shrink-0',
      collapsed ? 'w-14' : 'w-56'
    )}>
      {/* Logo */}
      <div className={clsx(
        'flex items-center gap-2.5 px-3 py-4 border-b border-[var(--color-border)]',
        collapsed ? 'justify-center' : ''
      )}>
        <div className="w-7 h-7 rounded-md bg-[var(--color-accent)] flex items-center justify-center shrink-0">
          <ActivitySquare size={16} className="text-white" />
        </div>
        {!collapsed && (
          <span className="text-sm font-bold text-[var(--color-text-primary)] whitespace-nowrap">BotPulse</span>
        )}
      </div>

      {/* Nav */}
      <nav className="flex-1 py-2 overflow-y-auto">
        {NAV_ITEMS.map(({ key, icon: Icon, path }) => (
          <NavLink
            key={key}
            to={path}
            className={({ isActive }) => clsx(
              'flex items-center gap-3 mx-2 my-0.5 px-2.5 py-2 rounded-md text-sm transition-colors group relative',
              isActive
                ? 'bg-[var(--color-accent)]/15 text-[var(--color-accent)] border-l-2 border-[var(--color-accent)] pl-[9px]'
                : 'text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)]'
            )}
            title={collapsed ? t(`nav.${key}`) : undefined}
          >
            <Icon size={16} className="shrink-0" />
            {!collapsed && (
              <span className="whitespace-nowrap">{t(`nav.${key}`)}</span>
            )}
            {/* Tooltip when collapsed */}
            {collapsed && (
              <div className="absolute left-full ml-2 px-2 py-1 bg-[var(--color-bg-hover)] text-[var(--color-text-primary)] text-xs rounded-md whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none z-50 border border-[var(--color-border)]">
                {t(`nav.${key}`)}
              </div>
            )}
          </NavLink>
        ))}
      </nav>

      {isAdmin && (
        <div className="border-t border-[var(--color-border)] p-3">
          {!collapsed && (
            <div className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)] mb-3">
              {t('admin.adminSection')}
            </div>
          )}
          <nav className="space-y-0.5">
            <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg)] overflow-hidden shadow-sm">
              <NavLink
                to={ADMIN_NAV_GROUP.parent.path}
                className={({ isActive }) => clsx(
                  'flex items-center gap-3 mx-2 my-0.5 px-2.5 py-2 rounded-md text-sm transition-colors group relative font-semibold',
                  isActive
                    ? 'bg-[var(--color-accent)]/15 text-[var(--color-accent)] border-l-2 border-[var(--color-accent)] pl-[9px]'
                    : 'text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)]'
                )}
                title={collapsed ? t(ADMIN_NAV_GROUP.parent.key) : undefined}
              >
                <ADMIN_NAV_GROUP.parent.icon size={16} className="shrink-0" />
                {!collapsed && <span className="whitespace-nowrap">{t(ADMIN_NAV_GROUP.parent.key)}</span>}
                {collapsed && (
                  <div className="absolute left-full ml-2 px-2 py-1 bg-[var(--color-bg-hover)] text-[var(--color-text-primary)] text-xs rounded-md whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none z-50 border border-[var(--color-border)]">
                    {t(ADMIN_NAV_GROUP.parent.key)}
                  </div>
                )}
              </NavLink>
              <div className="border-t border-[var(--color-border)] bg-[var(--color-bg-secondary)]">
                {ADMIN_NAV_GROUP.children.map(({ key, icon: Icon, path, permission }) => (
                  <PermissionGate key={key} permission={permission}>
                    <NavLink
                      to={path}
                      className={({ isActive }) => clsx(
                        'flex items-center gap-3 mx-3 my-0.5 px-2.5 py-2 rounded-md text-sm transition-colors group relative',
                        isActive
                          ? 'bg-[var(--color-accent)]/15 text-[var(--color-accent)] border-l-2 border-[var(--color-accent)] pl-[9px]'
                          : 'text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)]'
                      )}
                      title={collapsed ? t(key) : undefined}
                    >
                      <Icon size={16} className="shrink-0" />
                      {!collapsed && <span className="whitespace-nowrap">{t(key)}</span>}
                      {collapsed && (
                        <div className="absolute left-full ml-2 px-2 py-1 bg-[var(--color-bg-hover)] text-[var(--color-text-primary)] text-xs rounded-md whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none z-50 border border-[var(--color-border)]">
                          {t(key)}
                        </div>
                      )}
                    </NavLink>
                  </PermissionGate>
                ))}
              </div>
            </div>
          </nav>
        </div>
      )}

      {/* Collapse button */}
      <div className="border-t border-[var(--color-border)] p-2">
        <button
          onClick={onToggle}
          className="w-full flex items-center justify-center p-2 text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] rounded-md transition-colors"
          title={collapsed ? 'Expand' : 'Collapse'}
        >
          <ChevronLeft size={16} className={clsx(
            'transition-transform duration-200',
            collapsed ? 'rotate-180' : ''
          )} />
        </button>
      </div>
    </aside>
  )
}

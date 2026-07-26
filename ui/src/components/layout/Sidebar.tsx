import { NavLink } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  LayoutDashboard, Bot, Server, Workflow, Briefcase,
  ListOrdered, ScrollText, BarChart2, Bell, ChevronLeft, ActivitySquare
} from 'lucide-react'
import { clsx } from 'clsx'

const NAV_ITEMS = [
  { key: 'dashboard', icon: LayoutDashboard, path: '/dashboard' },
  { key: 'robots',    icon: Bot,             path: '/robots'    },
  { key: 'machines',  icon: Server,          path: '/machines'  },
  { key: 'processes', icon: Workflow,        path: '/processes' },
  { key: 'jobs',      icon: Briefcase,       path: '/jobs'      },
  { key: 'queues',    icon: ListOrdered,     path: '/queues'    },
  { key: 'logs',      icon: ScrollText,      path: '/logs'      },
  { key: 'metrics',   icon: BarChart2,       path: '/metrics'   },
  { key: 'alerts',    icon: Bell,            path: '/alerts'    },
]

interface Props {
  collapsed: boolean
  onToggle: () => void
}

export function Sidebar({ collapsed, onToggle }: Props) {
  const { t } = useTranslation()

  return (
    <aside className={clsx(
      'flex flex-col bg-gray-900 border-r border-gray-700 h-screen sticky top-0 transition-all duration-200 shrink-0',
      collapsed ? 'w-14' : 'w-56'
    )}>
      {/* Logo */}
      <div className={clsx(
        'flex items-center gap-2.5 px-3 py-4 border-b border-gray-700',
        collapsed ? 'justify-center' : ''
      )}>
        <div className="w-7 h-7 rounded-md bg-accent flex items-center justify-center shrink-0">
          <ActivitySquare size={16} className="text-white" />
        </div>
        {!collapsed && (
          <span className="text-sm font-bold text-white whitespace-nowrap">BotPulse</span>
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
                ? 'bg-accent/15 text-accent border-l-2 border-accent pl-[9px]'
                : 'text-gray-400 hover:text-gray-200 hover:bg-gray-800'
            )}
            title={collapsed ? t(`nav.${key}`) : undefined}
          >
            <Icon size={16} className="shrink-0" />
            {!collapsed && (
              <span className="whitespace-nowrap">{t(`nav.${key}`)}</span>
            )}
            {/* Tooltip when collapsed */}
            {collapsed && (
              <div className="absolute left-full ml-2 px-2 py-1 bg-gray-800 text-gray-200 text-xs rounded-md whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none z-50 border border-gray-700">
                {t(`nav.${key}`)}
              </div>
            )}
          </NavLink>
        ))}
      </nav>

      {/* Collapse button */}
      <div className="border-t border-gray-700 p-2">
        <button
          onClick={onToggle}
          className="w-full flex items-center justify-center p-2 text-gray-500 hover:text-gray-300 hover:bg-gray-800 rounded-md transition-colors"
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

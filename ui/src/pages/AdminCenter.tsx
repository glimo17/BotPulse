import { NavLink, Outlet } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { LayoutDashboard, UserCog, Users, Settings2 } from 'lucide-react'
import { clsx } from 'clsx'
import { PermissionGate } from '@/components/PermissionGate'

const TABS = [
  { key: 'admin.overview', icon: LayoutDashboard, path: '/admin',          end: true,  permission: undefined },
  { key: 'admin.users',    icon: UserCog,         path: '/admin/users',    end: false, permission: 'Users.View' },
  { key: 'admin.roles',    icon: Users,           path: '/admin/roles',    end: false, permission: 'Roles.View' },
  { key: 'admin.settings', icon: Settings2,       path: '/admin/settings', end: false, permission: 'Settings.View' },
]

export default function AdminCenter() {
  const { t } = useTranslation()

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-xl font-bold text-[var(--color-text-primary)]">{t('admin.center')}</h1>
        <p className="text-sm text-[var(--color-text-muted)]">{t('admin.centerSubtitle')}</p>
      </div>

      {/* Tab bar */}
      <div className="flex items-center gap-1 border-b border-[var(--color-border)]">
        {TABS.map(({ key, icon: Icon, path, end, permission }) => {
          const tab = (
            <NavLink
              key={key}
              to={path}
              end={end}
              className={({ isActive }) => clsx(
                'flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-colors',
                isActive
                  ? 'border-[var(--color-accent)] text-[var(--color-accent)]'
                  : 'border-transparent text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]'
              )}
            >
              <Icon size={15} />
              {t(key)}
            </NavLink>
          )
          return permission
            ? <PermissionGate key={key} permission={permission}>{tab}</PermissionGate>
            : tab
        })}
      </div>

      {/* Active tab content */}
      <Outlet />
    </div>
  )
}

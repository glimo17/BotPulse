import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { UserCog, Users, Settings2 } from 'lucide-react'
import { PermissionGate } from '@/components/PermissionGate'

export default function AdminDashboard() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  const cards = [
    { key: 'admin.users',    icon: UserCog,   description: 'admin.usersSubtitle',    path: '/admin/users',    permission: 'Users.View' },
    { key: 'admin.roles',    icon: Users,     description: 'admin.rolesDescription', path: '/admin/roles',    permission: 'Roles.View' },
    { key: 'admin.settings', icon: Settings2, description: 'admin.settingsDescription', path: '/admin/settings', permission: 'Settings.View' },
  ]

  return (
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
      {cards.map(({ key, icon: Icon, description, path, permission }) => (
        <PermissionGate key={key} permission={permission}>
          <button
            onClick={() => navigate(path)}
            className="text-left rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-secondary)] p-6 hover:border-[var(--color-accent)] hover:bg-[var(--color-bg-hover)] transition-colors group"
          >
            <div className="flex items-center gap-3 mb-3">
              <div className="w-9 h-9 rounded-lg bg-[var(--color-accent)]/10 flex items-center justify-center">
                <Icon size={18} className="text-[var(--color-accent)]" />
              </div>
              <h2 className="text-sm font-semibold text-[var(--color-text-primary)]">{t(key)}</h2>
            </div>
            <p className="text-sm text-[var(--color-text-muted)]">{t(description)}</p>
          </button>
        </PermissionGate>
      ))}
    </div>
  )
}

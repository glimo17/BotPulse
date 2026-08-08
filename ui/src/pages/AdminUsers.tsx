import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { UserCheck, UserX, KeyRound, Shield, Plus, Search } from 'lucide-react'
import api from '@/lib/api'
import { PermissionGate } from '@/components/PermissionGate'

interface User {
  id: string
  userName: string
  email: string
  roles: string[]
  isActive: boolean
  authProvider: string
  lastLoginUtc: string | null
}

interface Role {
  id: string
  name: string
  isSystemRole: boolean
}

// ── Assign Roles Modal ────────────────────────────────────────────────────────

interface AssignRolesModalProps {
  user: User
  allRoles: Role[]
  onClose: () => void
  onSave: (roleIds: string[]) => void
  isSaving: boolean
}

function AssignRolesModal({ user, allRoles, onClose, onSave, isSaving }: AssignRolesModalProps) {
  const { t } = useTranslation()
  const [selected, setSelected] = useState<Set<string>>(
    new Set(allRoles.filter(r => user.roles.includes(r.name)).map(r => r.id))
  )

  const toggle = (id: string) =>
    setSelected(prev => { const n = new Set(prev); n.has(id) ? n.delete(id) : n.add(id); return n })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="w-full max-w-sm rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-secondary)] p-6 shadow-xl flex flex-col gap-4">
        <h2 className="text-lg font-bold text-[var(--color-text-primary)]">
          {t('admin.assignRoles')} — {user.userName}
        </h2>
        <div className="flex flex-col gap-2 max-h-60 overflow-y-auto">
          {allRoles.map(role => (
            <label key={role.id} className="flex items-center gap-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] px-3 py-2 cursor-pointer hover:bg-[var(--color-bg-hover)]">
              <input type="checkbox" checked={selected.has(role.id)} onChange={() => toggle(role.id)} className="accent-[var(--color-accent)]" />
              <span className="text-sm text-[var(--color-text-primary)]">{role.name}</span>
              {role.isSystemRole && <span className="ml-auto text-xs text-[var(--color-text-muted)]">{t('admin.systemRole')}</span>}
            </label>
          ))}
        </div>
        <div className="flex justify-end gap-2">
          <button onClick={onClose} className="rounded-lg border border-[var(--color-border)] px-4 py-2 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-hover)]">{t('common.cancel')}</button>
          <button onClick={() => onSave(Array.from(selected))} disabled={isSaving} className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white disabled:opacity-50 hover:opacity-90">
            {isSaving ? t('common.saving') : t('common.save')}
          </button>
        </div>
      </div>
    </div>
  )
}

// ── Create User Modal ─────────────────────────────────────────────────────────

interface CreateUserModalProps {
  onClose: () => void
  onSave: (userName: string, email: string, password: string) => void
  isSaving: boolean
  error: string | null
}

function CreateUserModal({ onClose, onSave, isSaving, error }: CreateUserModalProps) {
  const { t } = useTranslation()
  const [userName, setUserName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="w-full max-w-sm rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-secondary)] p-6 shadow-xl flex flex-col gap-4">
        <h2 className="text-lg font-bold text-[var(--color-text-primary)]">{t('admin.createUser')}</h2>
        {error && <p className="text-sm text-red-400">{error}</p>}
        <input className="rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] px-3 py-2 text-sm" placeholder={t('auth.username')} value={userName} onChange={e => setUserName(e.target.value)} />
        <input className="rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] px-3 py-2 text-sm" placeholder="Email" type="email" value={email} onChange={e => setEmail(e.target.value)} />
        <input className="rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] px-3 py-2 text-sm" placeholder={t('auth.password')} type="password" value={password} onChange={e => setPassword(e.target.value)} />
        <div className="flex justify-end gap-2">
          <button onClick={onClose} className="rounded-lg border border-[var(--color-border)] px-4 py-2 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-hover)]">{t('common.cancel')}</button>
          <button onClick={() => onSave(userName, email, password)} disabled={!userName || !email || !password || isSaving} className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white disabled:opacity-50 hover:opacity-90">
            {isSaving ? t('common.saving') : t('common.save')}
          </button>
        </div>
      </div>
    </div>
  )
}

// ── Main Page ─────────────────────────────────────────────────────────────────

export default function AdminUsers() {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const [search, setSearch] = useState('')
  const [assignTarget, setAssignTarget] = useState<User | null>(null)
  const [showCreate, setShowCreate] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const { data: users = [], isLoading } = useQuery<User[]>({
    queryKey: ['adminUsers'],
    queryFn: () => api.get<User[]>('/users').then(r => r.data),
    staleTime: 30_000,
  })

  const { data: allRoles = [] } = useQuery<Role[]>({
    queryKey: ['adminRoles'],
    queryFn: () => api.get<Role[]>('/roles').then(r => r.data),
    staleTime: 60_000,
  })

  const enableMutation = useMutation({
    mutationFn: (id: string) => api.put(`/users/${id}/enable`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['adminUsers'] }),
    onError: (e: any) => setError(e.response?.data?.error ?? t('common.error')),
  })

  const disableMutation = useMutation({
    mutationFn: (id: string) => api.put(`/users/${id}/disable`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['adminUsers'] }),
    onError: (e: any) => setError(e.response?.data?.error ?? t('common.error')),
  })

  const assignRolesMutation = useMutation({
    mutationFn: ({ id, roleIds }: { id: string; roleIds: string[] }) =>
      api.put(`/users/${id}/roles`, { roleIds }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['adminUsers'] }); setAssignTarget(null) },
    onError: (e: any) => setError(e.response?.data?.error ?? t('common.error')),
  })

  const createMutation = useMutation({
    mutationFn: (body: { userName: string; email: string; password: string }) =>
      api.post('/users', body),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['adminUsers'] }); setShowCreate(false) },
    onError: (e: any) => setError(e.response?.data?.error ?? t('common.error')),
  })

  const filtered = users.filter(u =>
    u.userName.toLowerCase().includes(search.toLowerCase()) ||
    u.email.toLowerCase().includes(search.toLowerCase())
  )

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-[var(--color-text-primary)]">{t('admin.users')}</h1>
          <p className="text-sm text-[var(--color-text-muted)]">{t('admin.usersSubtitle')}</p>
        </div>
        <PermissionGate permission="Users.Create">
          <button onClick={() => { setShowCreate(true); setError(null) }}
            className="flex items-center gap-2 rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white hover:opacity-90 transition-opacity">
            <Plus size={15} />{t('admin.createUser')}
          </button>
        </PermissionGate>
      </div>

      {error && (
        <div className="rounded-lg border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-400">
          {error} <button className="ml-3 underline" onClick={() => setError(null)}>{t('common.close')}</button>
        </div>
      )}

      {/* Search */}
      <div className="relative">
        <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-[var(--color-text-muted)]" />
        <input className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-bg-secondary)] pl-9 pr-4 py-2 text-sm text-[var(--color-text-primary)] focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]"
          placeholder={t('common.search')} value={search} onChange={e => setSearch(e.target.value)} />
      </div>

      {/* Table */}
      <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-secondary)] overflow-hidden">
        {isLoading && <p className="p-4 text-sm text-[var(--color-text-muted)]">{t('common.loading')}</p>}
        {!isLoading && filtered.length === 0 && (
          <p className="p-4 text-sm text-[var(--color-text-muted)]">{t('admin.noUsers')}</p>
        )}
        {!isLoading && filtered.length > 0 && (
          <table className="w-full text-sm">
            <thead className="border-b border-[var(--color-border)] bg-[var(--color-bg)]">
              <tr>
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-muted)]">{t('auth.username')}</th>
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-muted)]">{t('admin.rolesCol')}</th>
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-muted)]">{t('common.status')}</th>
                <th className="px-4 py-3 text-left font-medium text-[var(--color-text-muted)]">{t('admin.lastLogin')}</th>
                <th className="px-4 py-3 text-right font-medium text-[var(--color-text-muted)]">{t('common.actions')}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--color-border)]">
              {filtered.map(user => (
                <tr key={user.id} className="hover:bg-[var(--color-bg-hover)] transition-colors">
                  <td className="px-4 py-3">
                    <div className="font-medium text-[var(--color-text-primary)]">{user.userName}</div>
                    <div className="text-xs text-[var(--color-text-muted)]">{user.email}</div>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap gap-1">
                      {user.roles.length > 0
                        ? user.roles.map(r => (
                          <span key={r} className="rounded-full bg-[var(--color-accent)]/10 px-2 py-0.5 text-xs text-[var(--color-accent)]">{r}</span>
                        ))
                        : <span className="text-xs text-[var(--color-text-muted)]">—</span>
                      }
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${user.isActive ? 'bg-green-500/10 text-green-400' : 'bg-red-500/10 text-red-400'}`}>
                      {user.isActive ? t('admin.active') : t('admin.disabled')}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-xs text-[var(--color-text-muted)]">
                    {user.lastLoginUtc ? new Date(user.lastLoginUtc).toLocaleString() : '—'}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex justify-end gap-1">
                      <PermissionGate permission="Users.Update">
                        <button onClick={() => { setAssignTarget(user); setError(null) }}
                          className="rounded-md p-1.5 text-[var(--color-text-muted)] hover:text-[var(--color-accent)] hover:bg-[var(--color-accent)]/10 transition-colors" title={t('admin.assignRoles')}>
                          <Shield size={14} />
                        </button>
                        {user.isActive
                          ? <button onClick={() => disableMutation.mutate(user.id)}
                              className="rounded-md p-1.5 text-[var(--color-text-muted)] hover:text-red-400 hover:bg-red-500/10 transition-colors" title={t('admin.disable')}>
                              <UserX size={14} />
                            </button>
                          : <button onClick={() => enableMutation.mutate(user.id)}
                              className="rounded-md p-1.5 text-[var(--color-text-muted)] hover:text-green-400 hover:bg-green-500/10 transition-colors" title={t('admin.enable')}>
                              <UserCheck size={14} />
                            </button>
                        }
                        {user.authProvider === 'Local' && (
                          <button className="rounded-md p-1.5 text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition-colors" title={t('admin.resetPassword')}>
                            <KeyRound size={14} />
                          </button>
                        )}
                      </PermissionGate>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {assignTarget && (
        <AssignRolesModal
          user={assignTarget}
          allRoles={allRoles}
          onClose={() => setAssignTarget(null)}
          onSave={roleIds => assignRolesMutation.mutate({ id: assignTarget.id, roleIds })}
          isSaving={assignRolesMutation.isPending}
        />
      )}

      {showCreate && (
        <CreateUserModal
          onClose={() => setShowCreate(false)}
          onSave={(userName, email, password) => createMutation.mutate({ userName, email, password })}
          isSaving={createMutation.isPending}
          error={error}
        />
      )}
    </div>
  )
}

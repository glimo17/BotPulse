import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Lock, Plus, Pencil, Trash2, ChevronDown, ChevronUp } from 'lucide-react'
import api from '@/lib/api'
import { PermissionGate } from '@/components/PermissionGate'

// ── Types ─────────────────────────────────────────────────────────────────────

interface Role {
  id: string
  name: string
  description: string
  isSystemRole: boolean
  permissions: string[]
}

// All 23 permissions grouped by area for the permission selector
const PERMISSION_GROUPS: Record<string, string[]> = {
  Dashboard:    ['Dashboard.View', 'Dashboard.Export'],
  Jobs:         ['Jobs.View', 'Jobs.Execute', 'Jobs.Cancel', 'Jobs.Retry'],
  Robots:       ['Robots.View', 'Robots.Restart'],
  Queues:       ['Queues.View'],
  Logs:         ['Logs.View'],
  Machines:     ['Machines.View'],
  Assets:       ['Assets.View'],
  Metrics:      ['Metrics.View'],
  Alerts:       ['Alerts.View', 'Alerts.Acknowledge'],
  Users:        ['Users.View', 'Users.Create', 'Users.Update', 'Users.Delete'],
  Roles:        ['Roles.View', 'Roles.Update'],
  Settings:     ['Settings.View', 'Settings.Edit'],
  Integrations: ['Integrations.Configure'],
}

// ── Role Modal ────────────────────────────────────────────────────────────────

interface RoleModalProps {
  initial?: Role | null
  onClose: () => void
  onSave: (name: string, description: string, permissions: string[]) => void
  isSaving: boolean
}

function RoleModal({ initial, onClose, onSave, isSaving }: RoleModalProps) {
  const { t } = useTranslation()
  const [name, setName] = useState(initial?.name ?? '')
  const [description, setDescription] = useState(initial?.description ?? '')
  const [selected, setSelected] = useState<Set<string>>(new Set(initial?.permissions ?? []))
  const [expanded, setExpanded] = useState<Record<string, boolean>>(
    Object.fromEntries(Object.keys(PERMISSION_GROUPS).map(k => [k, true]))
  )

  const togglePermission = (p: string) => {
    setSelected(prev => {
      const next = new Set(prev)
      next.has(p) ? next.delete(p) : next.add(p)
      return next
    })
  }

  const toggleGroup = (area: string) => {
    const perms = PERMISSION_GROUPS[area]
    const allSelected = perms.every(p => selected.has(p))
    setSelected(prev => {
      const next = new Set(prev)
      allSelected ? perms.forEach(p => next.delete(p)) : perms.forEach(p => next.add(p))
      return next
    })
  }

  const toggleExpand = (area: string) =>
    setExpanded(prev => ({ ...prev, [area]: !prev[area] }))

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="w-full max-w-lg rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-secondary)] p-6 shadow-xl flex flex-col gap-4 max-h-[90vh] overflow-y-auto">
        <h2 className="text-lg font-bold text-[var(--color-text-primary)]">
          {initial ? t('admin.editRole') : t('admin.createRole')}
        </h2>

        {/* Name */}
        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-[var(--color-text-secondary)]">{t('common.name')}</label>
          <input
            className="rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] px-3 py-2 text-sm text-[var(--color-text-primary)] focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]"
            value={name}
            onChange={e => setName(e.target.value)}
            placeholder="e.g. Finance Supervisor"
          />
        </div>

        {/* Description */}
        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium text-[var(--color-text-secondary)]">{t('common.description')}</label>
          <input
            className="rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] px-3 py-2 text-sm text-[var(--color-text-primary)] focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]"
            value={description}
            onChange={e => setDescription(e.target.value)}
            placeholder="Optional description"
          />
        </div>

        {/* Permission selector */}
        <div className="flex flex-col gap-2">
          <div className="flex items-center justify-between">
            <label className="text-sm font-medium text-[var(--color-text-secondary)]">{t('admin.permissions')}</label>
            <span className="text-xs text-[var(--color-text-muted)]">{selected.size} {t('admin.selected')}</span>
          </div>
          <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] divide-y divide-[var(--color-border)]">
            {Object.entries(PERMISSION_GROUPS).map(([area, perms]) => {
              const allSelected = perms.every(p => selected.has(p))
              const someSelected = perms.some(p => selected.has(p))
              return (
                <div key={area}>
                  <div className="flex items-center justify-between px-3 py-2">
                    <button
                      type="button"
                      className="flex items-center gap-2 text-sm font-medium text-[var(--color-text-primary)]"
                      onClick={() => toggleExpand(area)}
                    >
                      {expanded[area] ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
                      {area}
                    </button>
                    <input
                      type="checkbox"
                      checked={allSelected}
                      ref={el => { if (el) el.indeterminate = someSelected && !allSelected }}
                      onChange={() => toggleGroup(area)}
                      className="accent-[var(--color-accent)]"
                    />
                  </div>
                  {expanded[area] && (
                    <div className="px-6 pb-2 grid grid-cols-2 gap-1">
                      {perms.map(p => (
                        <label key={p} className="flex items-center gap-2 text-xs text-[var(--color-text-secondary)] cursor-pointer">
                          <input
                            type="checkbox"
                            checked={selected.has(p)}
                            onChange={() => togglePermission(p)}
                            className="accent-[var(--color-accent)]"
                          />
                          {p.split('.')[1]}
                        </label>
                      ))}
                    </div>
                  )}
                </div>
              )
            })}
          </div>
        </div>

        {/* Actions */}
        <div className="flex justify-end gap-2 pt-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-[var(--color-border)] px-4 py-2 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-hover)] transition-colors"
          >
            {t('common.cancel')}
          </button>
          <button
            type="button"
            disabled={!name.trim() || isSaving}
            onClick={() => onSave(name.trim(), description.trim(), Array.from(selected))}
            className="rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white disabled:opacity-50 hover:opacity-90 transition-opacity"
          >
            {isSaving ? t('common.saving') : t('common.save')}
          </button>
        </div>
      </div>
    </div>
  )
}

// ── Main Page ─────────────────────────────────────────────────────────────────

export default function AdminRoles() {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const [modalRole, setModalRole] = useState<Role | null | 'new'>('new' as any)
  const [showModal, setShowModal] = useState(false)
  const [deleteTarget, setDeleteTarget] = useState<Role | null>(null)
  const [error, setError] = useState<string | null>(null)

  const { data: roles = [], isLoading, isError } = useQuery<Role[]>({
    queryKey: ['adminRoles'],
    queryFn: () => api.get<Role[]>('/roles').then(r => r.data),
    staleTime: 60_000,
  })

  const createMutation = useMutation({
    mutationFn: (body: { name: string; description: string; permissions: string[] }) =>
      api.post('/roles', body),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['adminRoles'] }); setShowModal(false) },
    onError: (e: any) => setError(e.response?.data?.error ?? t('common.error')),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, ...body }: { id: string; name: string; description: string; permissions: string[] }) =>
      api.put(`/roles/${id}`, body),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['adminRoles'] }); setShowModal(false) },
    onError: (e: any) => setError(e.response?.data?.error ?? t('common.error')),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/roles/${id}`),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['adminRoles'] }); setDeleteTarget(null) },
    onError: (e: any) => setError(e.response?.data?.error ?? t('common.error')),
  })

  const handleSave = (name: string, description: string, permissions: string[]) => {
    setError(null)
    if (modalRole && typeof modalRole === 'object') {
      updateMutation.mutate({ id: modalRole.id, name, description, permissions })
    } else {
      createMutation.mutate({ name, description, permissions })
    }
  }

  const isSaving = createMutation.isPending || updateMutation.isPending

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-[var(--color-text-primary)]">{t('admin.roles')}</h1>
          <p className="text-sm text-[var(--color-text-muted)]">{t('admin.rolesSubtitle')}</p>
        </div>
        <PermissionGate permission="Roles.Update">
          <button
            onClick={() => { setModalRole(null); setShowModal(true); setError(null) }}
            className="flex items-center gap-2 rounded-lg bg-[var(--color-accent)] px-4 py-2 text-sm font-medium text-white hover:opacity-90 transition-opacity"
          >
            <Plus size={15} />
            {t('admin.createRole')}
          </button>
        </PermissionGate>
      </div>

      {/* Error banner */}
      {error && (
        <div className="rounded-lg border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-400">
          {error}
          <button className="ml-3 underline" onClick={() => setError(null)}>{t('common.close')}</button>
        </div>
      )}

      {/* Role list */}
      <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-secondary)] p-4">
        {isLoading && <p className="text-sm text-[var(--color-text-muted)]">{t('common.loading')}</p>}
        {isError && <p className="text-sm text-red-400">{t('common.error')}</p>}
        {!isLoading && !isError && roles.length === 0 && (
          <p className="text-sm text-[var(--color-text-muted)]">{t('admin.noRoles')}</p>
        )}
        {!isLoading && !isError && roles.length > 0 && (
          <div className="space-y-3">
            {roles.map(role => (
              <div key={role.id} className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg)] p-4">
                <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
                  <div className="flex items-center gap-2">
                    {role.isSystemRole && <Lock size={14} className="text-[var(--color-text-muted)] shrink-0" />}
                    <div>
                      <h2 className="text-base font-semibold text-[var(--color-text-primary)]">{role.name}</h2>
                      <p className="text-xs text-[var(--color-text-muted)]">
                        {role.isSystemRole ? t('admin.systemRole') : t('admin.customRole')}
                        {role.description && ` · ${role.description}`}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="text-sm text-[var(--color-text-secondary)]">
                      {role.permissions.length} {t('admin.permissions')}
                    </span>
                    <PermissionGate permission="Roles.Update">
                      {!role.isSystemRole && (
                        <div className="flex gap-1">
                          <button
                            onClick={() => { setModalRole(role); setShowModal(true); setError(null) }}
                            className="rounded-md p-1.5 text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] transition-colors"
                            title={t('common.edit')}
                          >
                            <Pencil size={14} />
                          </button>
                          <button
                            onClick={() => { setDeleteTarget(role); setError(null) }}
                            className="rounded-md p-1.5 text-[var(--color-text-muted)] hover:text-red-400 hover:bg-red-500/10 transition-colors"
                            title={t('common.delete')}
                          >
                            <Trash2 size={14} />
                          </button>
                        </div>
                      )}
                    </PermissionGate>
                  </div>
                </div>
                {role.permissions.length > 0 && (
                  <div className="mt-3 flex flex-wrap gap-1">
                    {role.permissions.map(p => (
                      <span key={p} className="rounded-full bg-[var(--color-accent)]/10 px-2 py-0.5 text-xs text-[var(--color-accent)]">
                        {p}
                      </span>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Create / Edit modal */}
      {showModal && (
        <RoleModal
          initial={typeof modalRole === 'object' ? modalRole : null}
          onClose={() => setShowModal(false)}
          onSave={handleSave}
          isSaving={isSaving}
        />
      )}

      {/* Delete confirmation modal */}
      {deleteTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-sm rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-secondary)] p-6 shadow-xl flex flex-col gap-4">
            <h2 className="text-lg font-bold text-[var(--color-text-primary)]">{t('admin.deleteRole')}</h2>
            <p className="text-sm text-[var(--color-text-secondary)]">
              {t('admin.deleteRoleConfirm', { name: deleteTarget.name })}
            </p>
            <div className="flex justify-end gap-2">
              <button
                onClick={() => setDeleteTarget(null)}
                className="rounded-lg border border-[var(--color-border)] px-4 py-2 text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-hover)] transition-colors"
              >
                {t('common.cancel')}
              </button>
              <button
                onClick={() => deleteMutation.mutate(deleteTarget.id)}
                disabled={deleteMutation.isPending}
                className="rounded-lg bg-red-500 px-4 py-2 text-sm font-medium text-white disabled:opacity-50 hover:bg-red-600 transition-colors"
              >
                {deleteMutation.isPending ? t('common.deleting') : t('common.delete')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

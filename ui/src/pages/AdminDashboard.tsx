import { useTranslation } from 'react-i18next'

export default function AdminDashboard() {
  const { t } = useTranslation()

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-[var(--color-text-primary)]">{t('admin.dashboard')}</h1>
          <p className="text-sm text-[var(--color-text-muted)]">{t('admin.dashboardSubtitle')}</p>
        </div>
      </div>
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-secondary)] p-6">
          <h2 className="text-sm font-semibold text-[var(--color-text-primary)] mb-2">{t('admin.summary')}</h2>
          <p className="text-sm text-[var(--color-text-muted)]">{t('admin.summaryDescription')}</p>
        </div>
        <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-secondary)] p-6">
          <h2 className="text-sm font-semibold text-[var(--color-text-primary)] mb-2">{t('admin.roles')}</h2>
          <p className="text-sm text-[var(--color-text-muted)]">{t('admin.rolesDescription')}</p>
        </div>
        <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-secondary)] p-6">
          <h2 className="text-sm font-semibold text-[var(--color-text-primary)] mb-2">{t('admin.settings')}</h2>
          <p className="text-sm text-[var(--color-text-muted)]">{t('admin.settingsDescription')}</p>
        </div>
      </div>
    </div>
  )
}

import { useTranslation } from 'react-i18next'

export default function AdminSettings() {
  const { t } = useTranslation()

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-[var(--color-text-primary)]">{t('admin.settings')}</h1>
          <p className="text-sm text-[var(--color-text-muted)]">{t('admin.settingsSubtitle')}</p>
        </div>
      </div>
      <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-secondary)] p-6">
        <div className="text-sm text-[var(--color-text-muted)]">{t('admin.settingsDescription')}</div>
      </div>
    </div>
  )
}

import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { CopyableId } from '@/components/CopyableId'
import { StatusBadge } from '@/components/StatusBadge'
import { useDensity } from '@/contexts/DensityContext'
import api from '@/lib/api'
import { clsx } from 'clsx'

interface Process { externalId: string; name: string; version: string; publicationStatus: string; description?: string; compatibleRobotCount: number }

export default function Processes() {
  const { t } = useTranslation()
  const { cellPadding } = useDensity()
  const { data: processes = [], isLoading } = useQuery<Process[]>({
    queryKey: ['processes'],
    queryFn: () => api.get('/processes').then(r => r.data),
    staleTime: 300_000,
  })

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-xl font-bold text-[var(--color-text-primary)]">Procesos</h1>
        <p className="text-sm text-[var(--color-text-muted)] mt-0.5">{processes.length} procesos</p>
      </div>
      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-[var(--color-border)] bg-[var(--color-bg-secondary)]">
                <th className={clsx('text-left text-xs text-[var(--color-text-secondary)] font-medium', cellPadding)}>Nombre</th>
                <th className={clsx('text-left text-xs text-[var(--color-text-secondary)] font-medium hidden sm:table-cell', cellPadding)}>Versión</th>
                <th className={clsx('text-left text-xs text-[var(--color-text-secondary)] font-medium hidden md:table-cell', cellPadding)}>Estado</th>
                <th className={clsx('text-left text-xs text-[var(--color-text-secondary)] font-medium hidden lg:table-cell', cellPadding)}>Robots compatibles</th>
                <th className={clsx('text-left text-xs text-[var(--color-text-secondary)] font-medium', cellPadding)}>ID</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr><td colSpan={5} className="px-4 py-8 text-center text-[var(--color-text-muted)]">{t('common.loadingProcesses')}</td></tr>
              ) : processes.length === 0 ? (
                <tr><td colSpan={5} className="px-4 py-8 text-center text-[var(--color-text-muted)]">{t('common.noData')}</td></tr>
              ) : processes.map(p => (
                <tr key={p.externalId} className="border-b border-[var(--color-border)] hover:bg-[var(--color-bg-hover)] transition-colors">
                  <td className={clsx('text-[var(--color-text-primary)] font-medium', cellPadding)}>
                    <div>{p.name}</div>
                    {p.description && <div className="text-xs text-[var(--color-text-muted)] truncate max-w-[200px]">{p.description}</div>}
                  </td>
                  <td className={clsx('text-[var(--color-text-secondary)] font-mono text-xs hidden sm:table-cell', cellPadding)}>{p.version}</td>
                  <td className={clsx('hidden md:table-cell', cellPadding)}><StatusBadge status={p.publicationStatus} /></td>
                  <td className={clsx('text-[var(--color-text-secondary)] hidden lg:table-cell', cellPadding)}>{p.compatibleRobotCount}</td>
                  <td className={cellPadding}><CopyableId id={p.externalId} maxLength={12} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}

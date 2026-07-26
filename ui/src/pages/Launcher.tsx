import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Rocket, Search, AlertTriangle, Loader2, CheckCircle, XCircle } from 'lucide-react'
import { StatusBadge } from '@/components/StatusBadge'
import { CopyableId } from '@/components/CopyableId'
import api from '@/lib/api'

interface Process { externalId: string; name: string; version: string; publicationStatus: string; description?: string }
interface Robot { externalId: string; name: string; status: string }
interface ProcessParameter { name: string; type: string; isRequired: boolean; defaultValue?: string }
interface LaunchedJob { id: string; processName: string; robot: string; status: string; startedAt: Date }

export default function Launcher() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  // State
  const [selectedProcess, setSelectedProcess] = useState<string | null>(null)
  const [selectedRobot, setSelectedRobot] = useState<string | null>(null)
  const [params, setParams] = useState<Record<string, string>>({})
  const [recentJobs, setRecentJobs] = useState<LaunchedJob[]>([])
  const [processSearch, setProcessSearch] = useState('')
  const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' } | null>(null)

  // Queries
  const { data: processes = [] } = useQuery<Process[]>({
    queryKey: ['processes'],
    queryFn: () => api.get('/processes').then(r => r.data),
    staleTime: 60_000,
  })

  const { data: robots = [] } = useQuery<Robot[]>({
    queryKey: ['robots'],
    queryFn: () => api.get('/robots').then(r => r.data),
    staleTime: 60_000,
  })

  const { data: processParams = [] } = useQuery<ProcessParameter[]>({
    queryKey: ['process-params', selectedProcess],
    queryFn: () => api.get(`/processes/${selectedProcess}/parameters`).then(r => r.data),
    enabled: !!selectedProcess,
    staleTime: 120_000,
  })

  // Launch mutation
  const launchMutation = useMutation({
    mutationFn: () => api.post('/jobs', {
      processExternalId: selectedProcess,
      robotExternalId: selectedRobot || undefined,
      parameters: Object.fromEntries(Object.entries(params).filter(([, v]) => v !== '')),
      priority: 'Normal',
    }),
    onSuccess: (res) => {
      const processName = processes.find(p => p.externalId === selectedProcess)?.name ?? selectedProcess ?? ''
      const robotName = selectedRobot ? robots.find(r => r.externalId === selectedRobot)?.name ?? selectedRobot : 'Auto'
      setRecentJobs(prev => [{
        id: res.data.jobExternalId,
        processName,
        robot: robotName,
        status: 'Running',
        startedAt: new Date(),
      }, ...prev].slice(0, 5))
      setToast({ message: `${t('launcher.launched')}: ${res.data.jobExternalId}`, type: 'success' })
      setTimeout(() => setToast(null), 4000)
      void queryClient.invalidateQueries({ queryKey: ['jobs-dashboard'] })
    },
    onError: (err: Error) => {
      setToast({ message: `${t('launcher.error')}: ${err.message}`, type: 'error' })
      setTimeout(() => setToast(null), 5000)
    },
  })

  // Validation
  const missingRequired = processParams
    .filter(p => p.isRequired && !params[p.name])
    .map(p => p.name)

  const canLaunch = selectedProcess && missingRequired.length === 0 && !launchMutation.isPending

  // Filtered processes
  const publishedProcesses = processes.filter(p => p.publicationStatus === 'Published')
  const filteredProcesses = processSearch
    ? publishedProcesses.filter(p => p.name.toLowerCase().includes(processSearch.toLowerCase()))
    : publishedProcesses

  const handleProcessSelect = (id: string) => {
    setSelectedProcess(id)
    setParams({})
  }

  const handleParamChange = (name: string, value: string) => {
    setParams(prev => ({ ...prev, [name]: value }))
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-xl font-bold text-[var(--color-text-primary)]">{t('launcher.title')}</h1>
        <p className="text-sm text-[var(--color-text-muted)] mt-0.5">{t('launcher.subtitle')}</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left: Form */}
        <div className="lg:col-span-2 space-y-4">
          {/* Process Selector */}
          <div className="card p-4 space-y-3">
            <label className="text-xs font-medium text-[var(--color-text-secondary)] uppercase tracking-wide">
              {t('launcher.selectProcess')}
            </label>
            <div className="relative">
              <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-[var(--color-text-muted)]" />
              <input
                type="text"
                value={processSearch}
                onChange={e => setProcessSearch(e.target.value)}
                placeholder={t('common.search')}
                className="w-full pl-9 pr-3 py-2 bg-[var(--color-bg-primary)] border border-[var(--color-border)] rounded-md text-sm text-[var(--color-text-primary)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-1 focus:ring-[var(--color-accent)]"
              />
            </div>
            <div className="max-h-40 overflow-y-auto space-y-1">
              {filteredProcesses.map(proc => (
                <button
                  key={proc.externalId}
                  onClick={() => handleProcessSelect(proc.externalId)}
                  className={`w-full flex items-center justify-between px-3 py-2 rounded-md text-left text-xs transition-colors ${
                    selectedProcess === proc.externalId
                      ? 'bg-[var(--color-accent)]/15 text-[var(--color-accent)] border border-[var(--color-accent)]/30'
                      : 'hover:bg-[var(--color-bg-hover)] text-[var(--color-text-primary)]'
                  }`}
                >
                  <div>
                    <span className="font-medium">{proc.name}</span>
                    <span className="ml-2 text-[var(--color-text-muted)]">v{proc.version}</span>
                  </div>
                </button>
              ))}
              {filteredProcesses.length === 0 && (
                <p className="text-xs text-[var(--color-text-muted)] px-3 py-2">{t('common.noData')}</p>
              )}
            </div>
          </div>

          {/* Robot Selector */}
          <div className="card p-4 space-y-3">
            <label className="text-xs font-medium text-[var(--color-text-secondary)] uppercase tracking-wide">
              {t('launcher.selectRobot')}
            </label>
            <div className="space-y-1">
              <button
                onClick={() => setSelectedRobot(null)}
                className={`w-full flex items-center gap-2 px-3 py-2 rounded-md text-xs text-left transition-colors ${
                  selectedRobot === null
                    ? 'bg-[var(--color-accent)]/15 text-[var(--color-accent)] border border-[var(--color-accent)]/30'
                    : 'hover:bg-[var(--color-bg-hover)] text-[var(--color-text-primary)]'
                }`}
              >
                <Rocket size={13} />
                {t('launcher.automatic')}
              </button>
              {robots.map(robot => (
                <button
                  key={robot.externalId}
                  onClick={() => setSelectedRobot(robot.externalId)}
                  className={`w-full flex items-center justify-between px-3 py-2 rounded-md text-xs text-left transition-colors ${
                    selectedRobot === robot.externalId
                      ? 'bg-[var(--color-accent)]/15 text-[var(--color-accent)] border border-[var(--color-accent)]/30'
                      : 'hover:bg-[var(--color-bg-hover)] text-[var(--color-text-primary)]'
                  }`}
                >
                  <span>{robot.name}</span>
                  <StatusBadge status={robot.status} />
                </button>
              ))}
            </div>
            {selectedRobot && robots.find(r => r.externalId === selectedRobot)?.status === 'Offline' && (
              <div className="flex items-center gap-2 text-xs text-[var(--color-warning)] px-2">
                <AlertTriangle size={12} />
                {t('launcher.offlineWarning')}
              </div>
            )}
          </div>

          {/* Parameters Form */}
          {selectedProcess && processParams.length > 0 && (
            <div className="card p-4 space-y-3">
              <label className="text-xs font-medium text-[var(--color-text-secondary)] uppercase tracking-wide">
                {t('launcher.parameters')}
              </label>
              <div className="space-y-3">
                {processParams.map(param => (
                  <div key={param.name} className="space-y-1">
                    <label className="text-xs text-[var(--color-text-secondary)]">
                      {param.name}
                      {param.isRequired && <span className="text-[var(--color-error)] ml-0.5">*</span>}
                      <span className="ml-1 text-[var(--color-text-muted)]">({param.type})</span>
                    </label>
                    {param.type === 'Boolean' ? (
                      <select
                        value={params[param.name] ?? param.defaultValue ?? ''}
                        onChange={e => handleParamChange(param.name, e.target.value)}
                        className="w-full px-3 py-2 bg-[var(--color-bg-primary)] border border-[var(--color-border)] rounded-md text-sm text-[var(--color-text-primary)] focus:outline-none focus:ring-1 focus:ring-[var(--color-accent)]"
                      >
                        <option value="">—</option>
                        <option value="true">True</option>
                        <option value="false">False</option>
                      </select>
                    ) : (
                      <input
                        type={param.type === 'Int32' ? 'number' : param.type === 'DateTime' ? 'datetime-local' : 'text'}
                        value={params[param.name] ?? ''}
                        onChange={e => handleParamChange(param.name, e.target.value)}
                        placeholder={param.defaultValue ?? ''}
                        className="w-full px-3 py-2 bg-[var(--color-bg-primary)] border border-[var(--color-border)] rounded-md text-sm text-[var(--color-text-primary)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:ring-1 focus:ring-[var(--color-accent)]"
                      />
                    )}
                  </div>
                ))}
              </div>
              {missingRequired.length > 0 && (
                <p className="text-xs text-[var(--color-error)]">
                  {t('launcher.requiredMissing')}: {missingRequired.join(', ')}
                </p>
              )}
            </div>
          )}

          {/* Launch Button */}
          <button
            onClick={() => launchMutation.mutate()}
            disabled={!canLaunch}
            className="w-full flex items-center justify-center gap-2 px-4 py-3 bg-[var(--color-accent)] hover:bg-[var(--color-accent-hover)] text-white font-medium rounded-lg transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
          >
            {launchMutation.isPending ? (
              <Loader2 size={16} className="animate-spin" />
            ) : (
              <Rocket size={16} />
            )}
            {launchMutation.isPending ? t('launcher.launching') : t('launcher.launchButton')}
          </button>
        </div>

        {/* Right: Recent Jobs */}
        <div className="space-y-4">
          <div className="card">
            <div className="px-4 py-3 border-b border-[var(--color-border)]">
              <span className="text-sm font-medium text-[var(--color-text-primary)]">{t('launcher.recentJobs')}</span>
            </div>
            <div className="p-3 space-y-2 max-h-80 overflow-y-auto">
              {recentJobs.length === 0 ? (
                <p className="text-xs text-[var(--color-text-muted)] px-2 py-4 text-center">{t('launcher.noLaunches')}</p>
              ) : (
                recentJobs.map(job => (
                  <div key={job.id} className="flex items-center justify-between px-3 py-2 bg-[var(--color-bg-hover)] rounded-md">
                    <div className="min-w-0">
                      <CopyableId id={job.id} maxLength={12} />
                      <p className="text-xs text-[var(--color-text-muted)] truncate">{job.processName}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      {job.status === 'Running' ? (
                        <Loader2 size={12} className="text-[var(--color-running)] animate-spin" />
                      ) : job.status === 'Success' ? (
                        <CheckCircle size={12} className="text-[var(--color-success)]" />
                      ) : (
                        <XCircle size={12} className="text-[var(--color-error)]" />
                      )}
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Toast */}
      {toast && (
        <div className={`fixed bottom-4 right-4 z-50 px-4 py-3 rounded-lg border text-sm shadow-lg ${
          toast.type === 'success'
            ? 'bg-[var(--color-success)]/10 border-[var(--color-success)]/30 text-[var(--color-success)]'
            : 'bg-[var(--color-error)]/10 border-[var(--color-error)]/30 text-[var(--color-error)]'
        }`}>
          {toast.message}
        </div>
      )}
    </div>
  )
}

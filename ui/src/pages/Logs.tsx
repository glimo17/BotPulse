import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Copy, Check } from 'lucide-react'
import { clsx } from 'clsx'
import api from '@/lib/api'

interface Log { id: number; timestampUtc: string; severity: string; loggerName: string; message: string; jobExternalId?: string; providerName: string }
interface LogsResponse { items?: Log[] }

const SEV_STYLES: Record<string, string> = {
  Debug: 'text-gray-500', Info: 'text-accent', Warn: 'text-warning', Error: 'text-error', Fatal: 'text-error font-bold'
}
const SEVERITIES = ['All', 'Debug', 'Info', 'Warn', 'Error', 'Fatal']

function CopyLine({ text }: { text: string }) {
  const [copied, setCopied] = useState(false)
  const copy = async () => { await navigator.clipboard.writeText(text); setCopied(true); setTimeout(() => setCopied(false), 2000) }
  return (
    <button onClick={copy} className="opacity-0 group-hover:opacity-100 ml-2 shrink-0 transition-opacity">
      {copied ? <Check size={11} className="text-success" /> : <Copy size={11} className="text-gray-500" />}
    </button>
  )
}

export default function Logs() {
  const { t } = useTranslation()
  const [severity, setSeverity] = useState('All')
  const [keyword, setKeyword] = useState('')

  const params = new URLSearchParams({ pageSize: '200' })
  if (severity !== 'All') params.set('severity', severity)
  if (keyword) params.set('keyword', keyword)

  const { data, isLoading } = useQuery<Log[] | LogsResponse>({
    queryKey: ['logs', severity, keyword],
    queryFn: () => api.get(`/logs?${params}`).then(r => r.data),
    staleTime: 30_000,
  })

  const logs: Log[] = Array.isArray(data) ? data : (data as LogsResponse)?.items ?? []

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-xl font-bold text-gray-100">Logs</h1>
        <p className="text-sm text-gray-500 mt-0.5">{logs.length} entradas</p>
      </div>

      <div className="flex items-center gap-3 flex-wrap">
        <input
          value={keyword} onChange={e => setKeyword(e.target.value)}
          placeholder={t('common.search')}
          className="bg-gray-800 border border-gray-700 rounded-md px-3 py-1.5 text-sm text-gray-200 placeholder-gray-500 focus:outline-none focus:border-accent w-48 transition-colors"
        />
        <div className="flex gap-1.5 flex-wrap">
          {SEVERITIES.map(s => (
            <button key={s} onClick={() => setSeverity(s)}
              className={clsx('px-2.5 py-1 rounded-full text-xs border transition-colors',
                severity === s ? 'bg-accent/20 text-accent border-accent/40' : 'text-gray-400 border-gray-700 hover:text-gray-200 hover:bg-gray-800')}>
              {s}
            </button>
          ))}
        </div>
      </div>

      <div className="card overflow-hidden">
        <div className="overflow-x-auto max-h-[calc(100vh-280px)] overflow-y-auto">
          <table className="w-full text-xs font-mono">
            <thead className="sticky top-0 bg-gray-900 z-10">
              <tr className="border-b border-gray-700">
                <th className="px-3 py-2 text-left text-gray-400 font-medium w-36">Timestamp</th>
                <th className="px-3 py-2 text-left text-gray-400 font-medium w-16">Nivel</th>
                <th className="px-3 py-2 text-left text-gray-400 font-medium">Mensaje</th>
                <th className="px-3 py-2 text-left text-gray-400 font-medium w-28 hidden md:table-cell">Logger</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr><td colSpan={4} className="px-4 py-8 text-center text-gray-500">{t('common.loadingLogs')}</td></tr>
              ) : logs.length === 0 ? (
                <tr><td colSpan={4} className="px-4 py-8 text-center text-gray-500">{t('common.noData')}</td></tr>
              ) : logs.map(log => (
                <tr key={log.id} className="border-b border-gray-800 hover:bg-gray-800/40 group">
                  <td className="px-3 py-1.5 text-gray-500 whitespace-nowrap">{new Date(log.timestampUtc).toLocaleTimeString('es')}</td>
                  <td className={clsx('px-3 py-1.5 whitespace-nowrap', SEV_STYLES[log.severity] || 'text-gray-400')}>{log.severity}</td>
                  <td className="px-3 py-1.5 text-gray-300">
                    <div className="flex items-start">
                      <span className="break-all">{log.message}</span>
                      <CopyLine text={log.message} />
                    </div>
                  </td>
                  <td className="px-3 py-1.5 text-gray-500 hidden md:table-cell truncate max-w-[110px]">{log.loggerName}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}

import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { BarChart, Bar, LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from 'recharts'
import api from '@/lib/api'

interface MetricRollup { bucketStartUtc: string; metricName: string; avgValue: number; sumValue: number; countValue: number }

const CHART_TOOLTIP_STYLE = { backgroundColor: '#181b1f', border: '1px solid #2c3235', borderRadius: '6px', fontSize: '11px', color: '#d4dce6' }

export default function Metrics() {
  const { t } = useTranslation()

  const { data: successRate = [] } = useQuery<MetricRollup[]>({
    queryKey: ['metrics-success-rate'],
    queryFn: () => api.get('/metrics/rollups?metric=jobs.success_rate&granularity=Hourly').then(r => r.data),
    staleTime: 300_000,
  })

  const { data: jobsTotal = [] } = useQuery<MetricRollup[]>({
    queryKey: ['metrics-jobs-total'],
    queryFn: () => api.get('/metrics/rollups?metric=jobs.total&granularity=Hourly').then(r => r.data),
    staleTime: 300_000,
  })

  const { data: jobsFailed = [] } = useQuery<MetricRollup[]>({
    queryKey: ['metrics-jobs-failed'],
    queryFn: () => api.get('/metrics/rollups?metric=jobs.failed&granularity=Hourly').then(r => r.data),
    staleTime: 300_000,
  })

  const fmtBucket = (iso: string) => new Date(iso).toLocaleTimeString('es', { hour: '2-digit', minute: '2-digit' })

  const rateData  = successRate.map(d => ({ time: fmtBucket(d.bucketStartUtc), value: Math.round(d.avgValue) }))
  const totalData = jobsTotal.map((d, i) => ({
    time: fmtBucket(d.bucketStartUtc),
    total: d.sumValue,
    failed: jobsFailed[i]?.sumValue ?? 0,
  }))

  const latestRate = rateData.length ? rateData[rateData.length - 1].value : null
  const totalToday = totalData.reduce((a, b) => a + b.total, 0)

  // Suppress unused variable warning for t
  void t

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-bold text-gray-100">Métricas</h1>
        <p className="text-sm text-gray-500 mt-0.5">Operaciones RPA en tiempo real</p>
      </div>

      <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
        <div className="card p-4">
          <p className="text-xs text-gray-400 uppercase tracking-wide mb-1">Tasa de éxito</p>
          <p className="text-3xl font-bold text-gray-100">{latestRate !== null ? `${latestRate}%` : '—'}</p>
          <p className="text-xs text-gray-500 mt-1">Última hora</p>
        </div>
        <div className="card p-4">
          <p className="text-xs text-gray-400 uppercase tracking-wide mb-1">Jobs hoy</p>
          <p className="text-3xl font-bold text-gray-100">{totalToday}</p>
          <p className="text-xs text-gray-500 mt-1">Total acumulado</p>
        </div>
        <div className="card p-4 col-span-2 md:col-span-1">
          <p className="text-xs text-gray-400 uppercase tracking-wide mb-1">Datos disponibles</p>
          <p className="text-3xl font-bold text-gray-100">{rateData.length}</p>
          <p className="text-xs text-gray-500 mt-1">Buckets horarios</p>
        </div>
      </div>

      {rateData.length > 0 && (
        <div className="card p-4">
          <h2 className="text-sm font-medium text-gray-200 mb-4">Tasa de Éxito (%)</h2>
          <ResponsiveContainer width="100%" height={180}>
            <LineChart data={rateData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#2c3235" />
              <XAxis dataKey="time" tick={{ fill: '#6e7a86', fontSize: 10 }} />
              <YAxis domain={[0, 100]} tick={{ fill: '#6e7a86', fontSize: 10 }} />
              <Tooltip contentStyle={CHART_TOOLTIP_STYLE} />
              <Line type="monotone" dataKey="value" stroke="#73bf69" strokeWidth={2} dot={false} />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}

      {totalData.length > 0 && (
        <div className="card p-4">
          <h2 className="text-sm font-medium text-gray-200 mb-4">Jobs por Hora</h2>
          <ResponsiveContainer width="100%" height={180}>
            <BarChart data={totalData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#2c3235" />
              <XAxis dataKey="time" tick={{ fill: '#6e7a86', fontSize: 10 }} />
              <YAxis tick={{ fill: '#6e7a86', fontSize: 10 }} />
              <Tooltip contentStyle={CHART_TOOLTIP_STYLE} />
              <Bar dataKey="total" fill="#3d71e8" radius={[2, 2, 0, 0]} name="Total" />
              <Bar dataKey="failed" fill="#f2495c" radius={[2, 2, 0, 0]} name="Fallidos" />
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}

      {rateData.length === 0 && totalData.length === 0 && (
        <div className="card p-8 text-center text-gray-500 text-sm">
          Sin datos de métricas. Inicia el Worker para comenzar a recolectar datos.
        </div>
      )}
    </div>
  )
}

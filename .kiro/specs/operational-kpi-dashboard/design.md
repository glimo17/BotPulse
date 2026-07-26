# Design Document - KPI Dashboard Operacional

## Overview

Este documento describe el diseño técnico para añadir 9 KPIs operacionales al Dashboard y la página de Metrics de BotPulse. Toda la lógica es client-side: los cálculos derivan de las 4 APIs ya existentes. No se modifica el backend.

**Stack frontend:**
- React 19 + TypeScript 6
- TanStack Query v5 (data fetching)
- Recharts v3 (charts)
- Tailwind CSS v3 (estilos)
- Lucide React (iconos)
- React Router DOM v7 (navegación)

---

## Archivos Afectados

| Archivo | Acción |
|---------|--------|
| `ui/src/lib/kpiCalculations.ts` | **CREAR** — funciones puras de cálculo |
| `ui/src/components/KpiCard.tsx` | **CREAR** — componente de tarjeta KPI |
| `ui/src/pages/Dashboard.tsx` | **REESCRIBIR** — 8 KPI cards + 4 queries paralelas |
| `ui/src/pages/Metrics.tsx` | **MODIFICAR** — añadir Exception Breakdown donut chart |

---

## 1. Funciones de Cálculo Puras (`ui/src/lib/kpiCalculations.ts`)

Todas las funciones son puras (no side effects, no I/O) para facilitar testing.

### Interfaces de entrada

```typescript
// Re-utilizadas en Dashboard y kpiCalculations
export interface Robot {
  externalId: string
  name: string
  status: string            // 'Online' | 'Offline' | 'Idle' | 'Busy'
  lastHeartbeatUtc: string
  machineExternalId?: string
}

export interface Job {
  externalJobId: string
  processExternalId: string
  robotExternalId: string
  status: { value: string } // 'Success' | 'Failed' | 'Stopped' | 'Cancelled' | 'Running' | 'Pending'
  startTimeUtc: string
  duration?: string         // ISO 8601, e.g. "PT2M30S"
  errorType?: string        // 'BusinessException' | 'SystemException' | null
}

export interface Queue {
  name: string
  pendingItems: number
  processedItems: number
  failedItems: number
  totalItems: number
}

export interface Alert {
  id: string
  severity: string          // 'Info' | 'Warning' | 'Critical'
  acknowledged: boolean
  raisedAtUtc: string
  acknowledgedAtUtc?: string | null
}
```

### Funciones exportadas

```typescript
// 1. Tasa de Éxito Global
export function calculateSuccessRate(jobs: Job[]): number | null {
  if (jobs.length === 0) return null
  const success = jobs.filter(j => j.status?.value === 'Success').length
  return Math.round(success / jobs.length * 100)
}

// 2. Disponibilidad de la Flota
export function calculateFleetAvailability(robots: Robot[]): number | null {
  if (robots.length === 0) return null
  const available = robots.filter(r =>
    ['Online', 'Idle', 'Busy'].includes(r.status)
  ).length
  return Math.round(available / robots.length * 100)
}

// 3. Utilización de Robots
export function calculateRobotUtilization(robots: Robot[]): {
  rate: number | null
  busyCount: number
  totalCount: number
} {
  const totalCount = robots.length
  const busyCount = robots.filter(r => r.status === 'Busy').length
  const rate = totalCount > 0 ? Math.round(busyCount / totalCount * 100) : null
  return { rate, busyCount, totalCount }
}

// 4. Queue Backlog
export function calculateQueueBacklog(queues: Queue[]): number {
  return queues.reduce((sum, q) => sum + (q.pendingItems ?? 0), 0)
}

// 5. MTTA en minutos
export function calculateMtta(alerts: Alert[]): number | null {
  const acknowledged = alerts.filter(a => a.acknowledgedAtUtc != null)
  if (acknowledged.length === 0) return null
  const totalMs = acknowledged.reduce((sum, a) => {
    const raised = new Date(a.raisedAtUtc).getTime()
    const acked  = new Date(a.acknowledgedAtUtc!).getTime()
    return sum + (acked - raised)
  }, 0)
  return Math.round(totalMs / 60000 / acknowledged.length)
}

// 6. Tiempo promedio de ciclo en segundos
export function calculateAvgCycleTime(jobs: Job[]): number | null {
  const TERMINAL = ['Success', 'Failed', 'Stopped', 'Cancelled']
  const withDuration = jobs.filter(
    j => TERMINAL.includes(j.status?.value) && j.duration
  )
  if (withDuration.length === 0) return null
  const totalSeconds = withDuration.reduce((sum, j) => {
    return sum + parseDurationToSeconds(j.duration!)
  }, 0)
  return Math.round(totalSeconds / withDuration.length)
}

// 7. Desglose de excepciones
export function calculateExceptionBreakdown(jobs: Job[]): {
  businessExceptions: number
  systemExceptions: number
  other: number
  total: number
} {
  const failed = jobs.filter(j => j.status?.value === 'Failed')
  const businessExceptions = failed.filter(j => j.errorType === 'BusinessException').length
  const systemExceptions   = failed.filter(j => j.errorType === 'SystemException').length
  const other              = failed.length - businessExceptions - systemExceptions
  return { businessExceptions, systemExceptions, other, total: failed.length }
}

// 8. Formato MTTA
export function formatMtta(minutes: number | null): string {
  if (minutes === null) return '—'
  if (minutes < 60) return `${minutes}m`
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  return `${h}h ${m.toString().padStart(2, '0')}m`
}

// 9. Formato de color para porcentajes operacionales
export function getPercentageColor(value: number | null): string {
  if (value === null) return 'text-gray-400'
  if (value >= 90) return 'text-success'
  if (value >= 70) return 'text-warning'
  return 'text-error'
}

// Helper interno: parsea ISO 8601 duration a segundos
function parseDurationToSeconds(iso: string): number {
  const match = iso.match(/PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+(?:\.\d+)?)S)?/)
  if (!match) return 0
  const h = parseInt(match[1] || '0')
  const m = parseInt(match[2] || '0')
  const s = parseFloat(match[3] || '0')
  return h * 3600 + m * 60 + s
}
```

### Función `formatAvgCycleTime`

```typescript
export function formatAvgCycleTime(seconds: number | null): string {
  if (seconds === null) return '—'
  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds % 3600) / 60)
  const s = Math.floor(seconds % 60)
  if (h > 0) return `${h}h ${m}m`
  if (m > 0) return `${m}m ${s}s`
  return `${s}s`
}
```

---

## 2. Componente KpiCard (`ui/src/components/KpiCard.tsx`)

### Interface de Props

```typescript
import type { LucideIcon } from 'lucide-react'

export interface KpiCardProps {
  label: string           // Texto en mayúsculas pequeñas encima del valor
  value: string | number | null  // Valor principal; null → muestra "—"
  subtitle: string        // Texto pequeño debajo del valor
  icon: LucideIcon        // Icono Lucide
  iconColor: string       // Color Tailwind del icono, e.g. 'text-success'
  valueColor?: string     // Override color del valor, e.g. 'text-error'
  href?: string           // Ruta de navegación (hace el card clickable)
  pulse?: boolean         // Si true Y value > 0, muestra badge pulsante rojo
  trend?: {
    value: number         // Positivo = subida, negativo = bajada
    label: string         // Texto descriptivo, e.g. "vs. período anterior"
  }
}
```

### Implementación

```tsx
import { Link } from 'react-router-dom'
import { TrendingUp, TrendingDown } from 'lucide-react'
import type { KpiCardProps } from './KpiCard'

export function KpiCard({
  label, value, subtitle, icon: Icon, iconColor,
  valueColor, href, pulse, trend
}: KpiCardProps) {
  const displayValue = value === null || value === undefined ? '—' : value
  const showPulse = pulse && typeof value === 'number' && value > 0

  // Extrae la clase de color de fondo del icono desde iconColor
  // e.g. 'text-success' → 'bg-success/20'
  const iconBg = iconColor.replace('text-', 'bg-') + '/20'

  const content = (
    <div className="card p-5 h-full flex flex-col">
      <div className="flex items-center gap-3 mb-3">
        <div className={`w-8 h-8 rounded-lg ${iconBg} flex items-center justify-center shrink-0`}>
          <Icon size={16} className={iconColor} />
        </div>
        <span className="text-xs text-gray-400 uppercase tracking-wide font-medium">
          {label}
        </span>
        {showPulse && (
          <span className="relative flex h-2 w-2 ml-auto">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-error opacity-75" />
            <span className="relative inline-flex rounded-full h-2 w-2 bg-error" />
          </span>
        )}
      </div>
      <p className={`text-2xl font-bold ${valueColor ?? 'text-gray-100'}`}>
        {displayValue}
      </p>
      <p className="text-xs text-gray-500 mt-1">{subtitle}</p>
      {trend && (
        <div className="flex items-center gap-1 mt-2 text-xs text-gray-500">
          {trend.value > 0
            ? <TrendingUp size={12} className="text-success" />
            : <TrendingDown size={12} className="text-error" />
          }
          <span>{trend.label}</span>
        </div>
      )}
    </div>
  )

  if (href) {
    return (
      <Link to={href} className="block hover:ring-1 hover:ring-accent/40 rounded-lg transition-all">
        {content}
      </Link>
    )
  }
  return content
}
```

---

## 3. Dashboard.tsx — Rediseño Completo

### Interfaces de datos

```typescript
interface Robot    { externalId: string; name: string; status: string; machineExternalId?: string; lastHeartbeatUtc: string }
interface Job      { externalJobId: string; processExternalId: string; robotExternalId: string; status: { value: string }; startTimeUtc: string; duration?: string; errorType?: string }
interface Queue    { name: string; pendingItems: number; processedItems: number; failedItems: number; totalItems: number }
interface Alert    { id: string; severity: string; acknowledged: boolean; raisedAtUtc: string; acknowledgedAtUtc?: string | null }
interface JobsPage { items: Job[]; total: number }
```

### 4 Queries Paralelas

```typescript
const { data: robots,   refetch: refetchRobots }   = useQuery<Robot[]>({
  queryKey: ['robots'],
  queryFn:  () => api.get('/robots').then(r => r.data),
  staleTime: 60_000,
})

const { data: jobsData, refetch: refetchJobs } = useQuery<JobsPage>({
  queryKey: ['jobs-dashboard'],
  queryFn:  () => api.get('/jobs?pageSize=50&sortDesc=true').then(r => r.data),
  staleTime: 30_000,
})

const { data: queues,   refetch: refetchQueues }   = useQuery<Queue[]>({
  queryKey: ['queues-dashboard'],
  queryFn:  () => api.get('/queues').then(r => r.data),
  staleTime: 60_000,
})

const { data: alerts,   refetch: refetchAlerts }   = useQuery<Alert[]>({
  queryKey: ['alerts-dashboard'],
  queryFn:  () => api.get('/alerts').then(r => r.data),
  staleTime: 30_000,
})

const refetchAll = () => {
  void refetchRobots()
  void refetchJobs()
  void refetchQueues()
  void refetchAlerts()
}
```

### Derivación de KPIs

```typescript
const jobs   = jobsData?.items ?? []
const robotList  = robots  ?? []
const queueList  = queues  ?? []
const alertList  = alerts  ?? []

// KPI 1 — Success Rate
const successRate = robots !== undefined ? calculateSuccessRate(jobs) : null
const successRateColor = getPercentageColor(successRate)

// KPI 2 — Jobs Volume (breakdown)
const successCount  = jobs.filter(j => j.status?.value === 'Success').length
const failedCount   = jobs.filter(j => j.status?.value === 'Failed').length
const stoppedCount  = jobs.filter(j => ['Stopped','Cancelled'].includes(j.status?.value)).length

// KPI 3 — Avg Cycle Time
const avgCycleTime      = jobsData !== undefined ? calculateAvgCycleTime(jobs) : null
const avgCycleTimeLabel = formatAvgCycleTime(avgCycleTime)

// KPI 4 — Robot Utilization
const utilization = robots !== undefined ? calculateRobotUtilization(robotList) : { rate: null, busyCount: 0, totalCount: 0 }

// KPI 5 — Fleet Availability
const fleetAvail      = robots !== undefined ? calculateFleetAvailability(robotList) : null
const fleetAvailColor = getPercentageColor(fleetAvail)

// KPI 6 — Queue Backlog
const queueBacklog = queues !== undefined ? calculateQueueBacklog(queueList) : null

// KPI 7 — Exception Breakdown
const excBreakdown = jobsData !== undefined ? calculateExceptionBreakdown(jobs) : null

// KPI 8 — MTTA
const mttaMinutes = alerts !== undefined ? calculateMtta(alertList) : null
const mttaLabel   = formatMtta(mttaMinutes)

// KPI 9 — Critical Alerts
const criticalAlerts    = alertList.filter(a => a.severity === 'Critical' && !a.acknowledged).length
const unacknowledgedAll = alertList.filter(a => !a.acknowledged).length
```

### Layout del Dashboard

```
┌──────────────────────────────────────────────────────┐
│  Header: título + auto-refresh controls               │
├──────────────┬───────────────┬──────────────┬────────┤
│ Success Rate │ Jobs Volume   │ Fleet Avail  │ Alerts │  ← Fila 1
├──────────────┼───────────────┼──────────────┼────────┤
│ Avg Cycle T  │ Robot Utiliz. │ Queue Backlog│ MTTA   │  ← Fila 2
├──────────────┴───────────────┴──────────────┴────────┤
│  Robot Fleet Grid (existente)                         │
├──────────────────────────────────────────────────────┤
│  Recent Jobs Table (existente)                        │
└──────────────────────────────────────────────────────┘
```

### Mapa de KPI Cards → Props

| KPI | label | icon | iconColor | valueColor | href | pulse |
|-----|-------|------|-----------|------------|------|-------|
| Success Rate | "Tasa de Éxito" | `Activity` | `text-success` | `successRateColor` | `/jobs` | — |
| Jobs Volume | "Jobs Ejecutados" | `Briefcase` | `text-accent` | — | `/jobs` | — |
| Fleet Availability | "Disponibilidad" | `Server` | `text-success` | `fleetAvailColor` | `/robots` | — |
| Critical Alerts | "Alertas Críticas" | `Bell` | `text-error` | criticalAlerts>0 ? 'text-error' : 'text-gray-100' | `/alerts` | `true` |
| Avg Cycle Time | "Ciclo Promedio" | `Clock` | `text-accent` | — | `/jobs` | — |
| Robot Utilization | "Utilización" | `Bot` | `text-warning` | — | `/robots` | — |
| Queue Backlog | "Backlog Colas" | `ListOrdered` | `text-warning` | — | `/queues` | — |
| MTTA | "MTTA" | `Timer` | `text-accent` | — | `/alerts` | — |

---

## 4. Metrics.tsx — Exception Breakdown Donut

### Nueva query en Metrics.tsx

```typescript
const { data: jobsMetrics = [] } = useQuery<Job[]>({
  queryKey: ['metrics-jobs-breakdown'],
  queryFn:  () => api.get('/jobs?pageSize=50').then(r => r.data?.items ?? []),
  staleTime: 300_000,
})
```

### Datos del donut

```typescript
const breakdown = calculateExceptionBreakdown(jobsMetrics)

const donutData = [
  { name: 'BusinessException', value: breakdown.businessExceptions, color: '#f5a623' },
  { name: 'SystemException',   value: breakdown.systemExceptions,   color: '#f2495c' },
  ...(breakdown.other > 0
    ? [{ name: 'Otros',        value: breakdown.other,              color: '#6e7a86' }]
    : []),
].filter(d => d.value > 0)
```

### Componente PieChart de Recharts

```tsx
import { PieChart, Pie, Cell, Legend, Tooltip, ResponsiveContainer } from 'recharts'

// Dentro del render, DESPUÉS de los charts existentes:
{breakdown.total > 0 ? (
  <div className="card p-4">
    <h2 className="text-sm font-medium text-gray-200 mb-4">
      Desglose de Excepciones
    </h2>
    <div className="flex items-center gap-6">
      <ResponsiveContainer width="100%" height={180}>
        <PieChart>
          <Pie
            data={donutData}
            cx="50%"
            cy="50%"
            innerRadius={50}
            outerRadius={80}
            paddingAngle={3}
            dataKey="value"
          >
            {donutData.map((entry, i) => (
              <Cell key={i} fill={entry.color} />
            ))}
          </Pie>
          <Tooltip contentStyle={CHART_TOOLTIP_STYLE} />
          <Legend
            formatter={(value, entry) => (
              <span style={{ color: '#d4dce6', fontSize: '11px' }}>
                {value} ({entry.payload?.value})
              </span>
            )}
          />
        </PieChart>
      </ResponsiveContainer>
    </div>
  </div>
) : (
  <div className="card p-6 text-center text-gray-500 text-sm">
    Sin excepciones en el período
  </div>
)}
```

---

## 5. Funciones Helper Existentes (mantener en Dashboard.tsx)

Las funciones `formatDuration` y `timeAgo` ya existen en `Dashboard.tsx`. Se conservan tal cual.

```typescript
function formatDuration(iso?: string): string {
  if (!iso) return '—'
  const match = iso.match(/PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+(?:\.\d+)?)S)?/)
  if (!match) return iso
  const h = parseInt(match[1] || '0'), m = parseInt(match[2] || '0'), s = Math.floor(parseFloat(match[3] || '0'))
  if (h > 0) return `${h}h ${m}m`
  if (m > 0) return `${m}m ${s}s`
  return `${s}s`
}

function timeAgo(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime()
  const m = Math.floor(diff / 60000)
  if (m < 1) return 'ahora'
  if (m < 60) return `${m}m`
  return `${Math.floor(m / 60)}h`
}
```

---

## 6. Tema Visual (Colores)

| Propósito | Clase Tailwind | Hex |
|-----------|---------------|-----|
| Success / bueno | `text-success` | `#73bf69` |
| Warning / atención | `text-warning` | `#f5a623` |
| Error / crítico | `text-error` | `#f2495c` |
| Accent / neutro-activo | `text-accent` | `#3d71e8` |
| Texto primario | `text-gray-100` | |
| Texto secundario | `text-gray-400` / `text-gray-500` | |
| Card background | `card` (clase utilitaria) | `#1a1d21` |

---

## 7. Correctness Properties

### Property 1: Success Rate está en rango [0, 100]

**Validates: Requirements 3.1**

Para cualquier array de jobs no vacío, `calculateSuccessRate(jobs)` siempre devuelve un valor en `[0, 100]` o `null` si el array está vacío.

```typescript
// fast-check property (Vitest)
fc.assert(fc.property(
  fc.array(fc.record({
    status: fc.record({ value: fc.constantFrom('Success', 'Failed', 'Stopped', 'Running') })
  }), { minLength: 1 }),
  (jobs) => {
    const rate = calculateSuccessRate(jobs as Job[])
    return rate !== null && rate >= 0 && rate <= 100
  }
))
```

### Property 2: Fleet Availability ≤ 100%

**Validates: Requirements 7.1**

Para cualquier array de robots no vacío, `calculateFleetAvailability(robots)` nunca supera 100 ni baja de 0.

### Property 3: Queue Backlog es siempre ≥ 0

**Validates: Requirements 8.1**

Para cualquier array de colas, `calculateQueueBacklog(queues)` retorna un entero ≥ 0.

### Property 4: Exception counts sum equals failed jobs

**Validates: Requirements 9.1, 9.2**

Para cualquier array de jobs, `businessExceptions + systemExceptions + other === total` donde `total` es el número de jobs con `status.value === 'Failed'`.

### Property 5: MTTA null cuando no hay alertas reconocidas

**Validates: Requirements 10.1**

Si todos los alerts tienen `acknowledgedAtUtc === null`, `calculateMtta(alerts)` retorna `null`.

---

## 8. Notas de Implementación

1. **Orden de imports en Dashboard.tsx**: Lucide icons necesarios: `Activity`, `Briefcase`, `Server`, `Bell`, `Clock`, `Bot`, `ListOrdered`, `Timer`, `RefreshCw`, `Pause`, `Play`.

2. **El `queryKey` de queues y alerts debe ser diferente** del que usen otras páginas (usar `['queues-dashboard']` y `['alerts-dashboard']`) para evitar invalidaciones cruzadas no deseadas.

3. **El tipo `JobsPage`** ya existe en Dashboard.tsx como `{ items: Job[]; total: number }`. La query de Metrics debe acceder a `.data?.items ?? []` ya que el endpoint devuelve paginación.

4. **`KpiCard` usa la clase CSS `card`** que ya existe en el proyecto (aplica `bg-gray-850`, `border border-gray-700`, `rounded-lg`). No redefinir.

5. **El donut chart de Recharts** puede requerir importar `Cell` desde `recharts` además de `PieChart` y `Pie`. Recharts v3 es compatible con estos componentes.

6. **`getPercentageColor`** retorna una clase Tailwind completa (e.g. `'text-success'`), por lo que se puede usar directamente en `className`.

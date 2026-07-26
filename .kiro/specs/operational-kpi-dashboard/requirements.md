# Requisitos - KPI Dashboard Operacional (BotPulse)

## Introducción

Este spec describe la mejora del dashboard operacional de BotPulse para exponer **9 KPIs críticos** calculados en el cliente a partir de los datos ya disponibles en las 4 APIs existentes (`/robots`, `/jobs`, `/queues`, `/alerts`). No se requieren nuevos endpoints de backend.

El objetivo es dar al operador RPA visibilidad inmediata sobre el estado de la flota, el rendimiento de los jobs y la salud operacional del sistema.

---

## Requisitos Funcionales

### Sección 1: Componente KpiCard

#### Requisito 1: Tarjeta KPI Reutilizable

**Historia de Usuario:** Como operador, quiero tarjetas visuales de KPI con colores semánticos y subtexto explicativo, para interpretar el estado operacional de un vistazo.

##### Criterios de Aceptación

1. THE **KpiCard Component** SHALL accept the following props: `label` (string), `value` (string | number | null), `subtitle` (string), `icon` (LucideIcon), `iconColor` (string), `valueColor` (optional string to override value color), `href` (optional string for navigation), `pulse` (optional boolean for pulsing badge), `trend` (optional object with `value: number` and `label: string`).
2. WHEN `value` is `null` or data is loading, THE **KpiCard Component** SHALL display `"—"` (em dash) as the value.
3. WHEN `href` is provided, THE **KpiCard Component** SHALL render as a clickable element that navigates to the given route using `react-router-dom`.
4. WHEN `pulse` is `true` and `value` is greater than 0, THE **KpiCard Component** SHALL render a pulsing red dot indicator using CSS animation alongside the value.
5. THE **KpiCard Component** SHALL display the icon inside a rounded container with a background color derived from `iconColor` at 20% opacity.
6. WHEN `trend` is provided, THE **KpiCard Component** SHALL display a directional arrow icon (up if `trend.value > 0`, down if `< 0`) and the trend label in a subdued color below the subtitle.

---

### Sección 2: Dashboard - 8 KPI Cards

#### Requisito 2: Layout de 8 KPI Cards en 2 Filas

**Historia de Usuario:** Como operador, quiero ver los 8 KPIs principales organizados en 2 filas de 4 tarjetas, para tener visión completa del estado operacional en un solo pantallado.

##### Criterios de Aceptación

1. THE **Dashboard Page** SHALL display 8 KPI cards arranged in 2 rows of 4 cards using a responsive CSS grid (`grid-cols-2 lg:grid-cols-4`).
2. THE **Row 1** SHALL contain cards in this order: Success Rate, Jobs Volume, Fleet Availability, Critical Alerts.
3. THE **Row 2** SHALL contain cards in this order: Average Cycle Time, Robot Utilization, Queue Backlog, MTTA.
4. THE **Dashboard Page** SHALL fetch data from 4 parallel queries: `GET /api/v1/robots`, `GET /api/v1/jobs?pageSize=50&sortDesc=true`, `GET /api/v1/queues`, `GET /api/v1/alerts`.
5. WHEN any query is loading, THE **Dashboard Page** SHALL show `"—"` in the corresponding KPI cards without blocking the render of other cards.
6. ALL 8 KPI cards SHALL refresh automatically when the existing auto-refresh mechanism triggers (every 30 seconds).

---

#### Requisito 3: KPI — Tasa de Éxito Global (Success Rate)

**Historia de Usuario:** Como operador, quiero ver la tasa de éxito de los jobs con color semántico, para identificar inmediatamente si las automatizaciones están fallando.

##### Criterios de Aceptación

1. THE **Success Rate KPI** SHALL calculate the value as `Math.round(successJobs / totalJobs * 100)` where `successJobs` is the count of jobs with `status.value === 'Success'` and `totalJobs` is the count of all jobs returned by the query.
2. WHEN `totalJobs` is 0, THE **Success Rate KPI** SHALL display `"—"`.
3. WHEN the success rate is ≥ 90%, THE **Success Rate KPI** SHALL display the value in `text-success` color (`#73bf69`).
4. WHEN the success rate is between 70% and 89% (inclusive), THE **Success Rate KPI** SHALL display the value in `text-warning` color (`#f5a623`).
5. WHEN the success rate is below 70%, THE **Success Rate KPI** SHALL display the value in `text-error` color (`#f2495c`).
6. THE **Success Rate KPI** SHALL display the value with a `%` suffix (e.g., `"87%"`).
7. THE **Success Rate KPI card** SHALL navigate to `/jobs` when clicked.

---

#### Requisito 4: KPI — Volumen de Jobs Ejecutados

**Historia de Usuario:** Como operador, quiero ver el total de jobs con desglose por estado, para evaluar el throughput de la flota.

##### Criterios de Aceptación

1. THE **Jobs Volume KPI** SHALL display the total count of jobs returned by the `/jobs?pageSize=50` query as the main value.
2. THE **Jobs Volume KPI** subtitle SHALL show the breakdown: `"{successCount} ok · {failedCount} fail · {stoppedCount} stop"`.
3. `stoppedCount` SHALL be the count of jobs with `status.value === 'Stopped'` or `status.value === 'Cancelled'`.
4. THE **Jobs Volume KPI card** SHALL navigate to `/jobs` when clicked.

---

#### Requisito 5: KPI — Tiempo Promedio de Ciclo (Avg Cycle Time)

**Historia de Usuario:** Como operador, quiero ver el tiempo promedio de ejecución de los jobs, para detectar degradaciones de rendimiento.

##### Criterios de Aceptación

1. THE **Avg Cycle Time KPI** SHALL calculate the average `duration` only for jobs in terminal states (`Success`, `Failed`, `Stopped`, `Cancelled`) that have a non-null `duration` field.
2. THE **Avg Cycle Time KPI** SHALL parse ISO 8601 duration strings (e.g., `"PT2M30S"`) using the existing `formatDuration` function logic.
3. WHEN no jobs with duration exist, THE **Avg Cycle Time KPI** SHALL display `"—"`.
4. THE **Avg Cycle Time KPI** SHALL display the average duration in a human-readable format (e.g., `"2m 30s"`, `"1h 15m"`).
5. THE **Avg Cycle Time KPI card** SHALL navigate to `/jobs` when clicked.

---

#### Requisito 6: KPI — Utilización de Robots (Robot Utilization)

**Historia de Usuario:** Como operador, quiero ver qué porcentaje de los robots están actualmente ocupados, para saber si la flota está subutilizada o saturada.

##### Criterios de Aceptación

1. THE **Robot Utilization KPI** SHALL calculate the value as `Math.round(busyRobots / totalRobots * 100)` where `busyRobots` is the count of robots with `status === 'Busy'`.
2. WHEN `totalRobots` is 0, THE **Robot Utilization KPI** SHALL display `"—"`.
3. THE **Robot Utilization KPI** SHALL display the value with a `%` suffix.
4. THE **Robot Utilization KPI** subtitle SHALL show `"{busyCount} ocupados de {totalCount}"`.
5. THE **Robot Utilization KPI card** SHALL navigate to `/robots` when clicked.

---

#### Requisito 7: KPI — Disponibilidad de la Flota (Fleet Availability)

**Historia de Usuario:** Como operador, quiero ver el porcentaje de robots disponibles (operativos), para identificar si hay robots offline que requieren atención.

##### Criterios de Aceptación

1. THE **Fleet Availability KPI** SHALL calculate the value as `Math.round(availableRobots / totalRobots * 100)` where `availableRobots` is the count of robots with `status` in `['Online', 'Idle', 'Busy']`.
2. WHEN `totalRobots` is 0, THE **Fleet Availability KPI** SHALL display `"—"`.
3. WHEN the fleet availability is ≥ 90%, THE **Fleet Availability KPI** SHALL display the value in `text-success` color.
4. WHEN the fleet availability is between 70% and 89%, THE **Fleet Availability KPI** SHALL display the value in `text-warning` color.
5. WHEN the fleet availability is below 70%, THE **Fleet Availability KPI** SHALL display the value in `text-error` color.
6. THE **Fleet Availability KPI** SHALL display the value with a `%` suffix.
7. THE **Fleet Availability KPI card** SHALL navigate to `/robots` when clicked.

---

#### Requisito 8: KPI — Queue Backlog

**Historia de Usuario:** Como operador, quiero ver el total de ítems pendientes en todas las colas, para detectar acumulación de trabajo no procesado.

##### Criterios de Aceptación

1. THE **Queue Backlog KPI** SHALL calculate the value as the sum of `pendingItems` across all queues returned by `GET /api/v1/queues`.
2. WHEN no queues are returned or the array is empty, THE **Queue Backlog KPI** SHALL display `0`.
3. THE **Queue Backlog KPI** subtitle SHALL show `"{queueCount} colas activas"`.
4. THE **Queue Backlog KPI card** SHALL navigate to `/queues` when clicked.

---

#### Requisito 9: KPI — Tasa de Excepciones (Exception Rate)

**Historia de Usuario:** Como analista, quiero ver el desglose de tipos de excepción en los jobs fallidos, para priorizar si son errores de proceso (BusinessException) o de infraestructura (SystemException).

##### Criterios de Aceptación

1. THE **Exception Rate KPI** SHALL count `businessExceptions` as the number of jobs with `status.value === 'Failed'` and `errorType === 'BusinessException'`.
2. THE **Exception Rate KPI** SHALL count `systemExceptions` as the number of jobs with `status.value === 'Failed'` and `errorType === 'SystemException'`.
3. THE **Exception Rate KPI** main value SHALL display the total failed job count.
4. THE **Exception Rate KPI** subtitle SHALL show `"B:{businessCount} / S:{systemCount}"` where B = BusinessException, S = SystemException.
5. THE **Exception Rate KPI card** SHALL navigate to `/jobs` when clicked.

---

#### Requisito 10: KPI — MTTA (Mean Time to Acknowledge)

**Historia de Usuario:** Como responsable de operaciones, quiero ver el tiempo promedio de reconocimiento de alertas, para medir la capacidad de respuesta del equipo.

##### Criterios de Aceptación

1. THE **MTTA KPI** SHALL calculate the average time in minutes between `raisedAtUtc` and `acknowledgedAtUtc` for all alerts where `acknowledgedAtUtc` is not null.
2. THE **MTTA KPI** calculation SHALL use: `Math.round(sum((acknowledgedAt - raisedAt) / 60000) / count)`.
3. WHEN no alerts have been acknowledged (`acknowledgedAtUtc` is null for all), THE **MTTA KPI** SHALL display `"—"`.
4. THE **MTTA KPI** SHALL display the value in minutes with suffix `"min"` (e.g., `"14 min"`). WHEN the value exceeds 60 minutes, it SHALL display in hours and minutes format (e.g., `"1h 05m"`).
5. THE **MTTA KPI card** SHALL navigate to `/alerts` when clicked.

---

#### Requisito 11: KPI — Alertas Críticas Activas

**Historia de Usuario:** Como operador, quiero ver el número de alertas críticas no reconocidas con una señal visual pulsante, para detectar inmediatamente situaciones de emergencia.

##### Criterios de Aceptación

1. THE **Critical Alerts KPI** SHALL count alerts with `severity === 'Critical'` and `acknowledged === false`.
2. WHEN the critical alert count is greater than 0, THE **Critical Alerts KPI** SHALL display a pulsing red dot indicator.
3. THE **Critical Alerts KPI** value SHALL be displayed in `text-error` color when count > 0 and `text-gray-100` when count is 0.
4. THE **Critical Alerts KPI** subtitle SHALL show the total unacknowledged alert count: `"{total} sin atender"`.
5. THE **Critical Alerts KPI card** SHALL navigate to `/alerts` when clicked.

---

### Sección 3: Metrics Page — Exception Breakdown Chart

#### Requisito 12: Gráfico de Desglose de Excepciones

**Historia de Usuario:** Como analista, quiero un gráfico circular (donut) que muestre la distribución de tipos de excepción en los jobs fallidos, para análisis visual rápido.

##### Criterios de Aceptación

1. THE **Metrics Page** SHALL include an Exception Breakdown section after the existing charts.
2. THE **Exception Breakdown Chart** SHALL use `PieChart` from Recharts rendered as a donut (with `innerRadius` set to approximately 60% of the outer radius).
3. THE **Exception Breakdown Chart** SHALL show three segments: `BusinessException` in `#f5a623` (warning color), `SystemException` in `#f2495c` (error color), and `Other` (failed jobs without recognized errorType) in `#6e7a86`.
4. THE **Exception Breakdown Chart** SHALL fetch job data via a new query to `GET /api/v1/jobs?pageSize=50` on the Metrics page.
5. WHEN there are no failed jobs, THE **Exception Breakdown Chart** SHALL show an empty state message: `"Sin excepciones en el período"`.
6. THE **Exception Breakdown Chart** SHALL display a legend with count and percentage for each segment.
7. THE **Exception Breakdown Chart** SHALL use the same `CHART_TOOLTIP_STYLE` as the existing charts in `Metrics.tsx`.

---

### Sección 4: Funciones de Cálculo Puras (kpiCalculations.ts)

#### Requisito 13: Funciones Puras Testeables para KPIs

**Historia de Usuario:** Como desarrollador, quiero que los cálculos de KPIs estén aislados en funciones puras, para que sean testeables y reutilizables.

##### Criterios de Aceptación

1. A **`kpiCalculations.ts`** utility file SHALL be created in `ui/src/lib/` containing all KPI calculation functions as pure functions with no side effects.
2. THE **`calculateSuccessRate`** function SHALL accept `jobs: Job[]` and return `number | null`.
3. THE **`calculateFleetAvailability`** function SHALL accept `robots: Robot[]` and return `number | null`.
4. THE **`calculateRobotUtilization`** function SHALL accept `robots: Robot[]` and return `{ rate: number | null; busyCount: number; totalCount: number }`.
5. THE **`calculateQueueBacklog`** function SHALL accept `queues: Queue[]` and return `number`.
6. THE **`calculateMtta`** function SHALL accept `alerts: Alert[]` and return `number | null` (in minutes).
7. THE **`calculateAvgCycleTime`** function SHALL accept `jobs: Job[]` and return `number | null` (in seconds).
8. THE **`calculateExceptionBreakdown`** function SHALL accept `jobs: Job[]` and return `{ businessExceptions: number; systemExceptions: number; other: number; total: number }`.
9. THE **`formatMtta`** function SHALL accept `minutes: number | null` and return a formatted string (`"—"`, `"Xm"`, or `"Xh Ym"`).

---

### Sección 5: Navegación Clickable

#### Requisito 14: KPI Cards Navegables

**Historia de Usuario:** Como operador, quiero hacer clic en cualquier KPI card para ir directamente al detalle relevante, para investigar rápidamente anomalías.

##### Criterios de Aceptación

1. THE **KpiCard Component** WHEN `href` is provided SHALL wrap the entire card in a `Link` component from `react-router-dom`.
2. WHEN a user clicks a card with `href`, THE **Browser** SHALL navigate to the specified route without full page reload.
3. THE **KpiCard Component** WHEN `href` is provided SHALL apply a `hover:ring-1 hover:ring-accent/40` CSS class to provide visual feedback on hover.
4. WHEN `href` is not provided, THE **KpiCard Component** SHALL render as a standard non-interactive `div`.

---

## Requisitos No Funcionales

- Los 4 queries del dashboard SHALL ejecutarse en paralelo (no en cascada) para minimizar el tiempo de carga.
- El componente `KpiCard` SHALL ser un componente React puro sin estado interno.
- Las funciones de cálculo en `kpiCalculations.ts` SHALL ser funciones puras sin dependencias externas.
- El build de TypeScript (`npm run build`) SHALL pasar sin errores de tipo.
- Los nuevos componentes SHALL seguir el patrón de tema Grafana dark existente: `bg-gray-850`, `text-gray-100`, etc.
- La página `Metrics.tsx` SHALL mantener los charts existentes (Success Rate line chart, Jobs por Hora bar chart) íntegros, añadiendo el Exception Breakdown al final.

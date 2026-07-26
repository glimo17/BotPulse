# Plan de Implementación - KPI Dashboard Operacional

## Introducción

Este plan describe la implementación de los 9 KPIs operacionales en el frontend de BotPulse. Todas las tareas son exclusivamente frontend (React + TypeScript). No se modifica el backend.

**Archivos objetivo:**
- `ui/src/lib/kpiCalculations.ts` (nuevo)
- `ui/src/components/KpiCard.tsx` (nuevo)
- `ui/src/pages/Dashboard.tsx` (reescribir)
- `ui/src/pages/Metrics.tsx` (modificar)

## Convenciones

- **Numeración:** `Tarea.Subtarea` (ej. `1.1`).
- **Estado:** cada elemento es marcable con `- [ ]`.
- **Trazabilidad:** cada tarea referencia requisitos con `_Requirements: N_`.
- Los tests se escriben como subtarea de la misma tarea que implementa la funcionalidad.

---

## Tarea 1: Crear funciones puras de cálculo de KPIs

Crear el archivo `ui/src/lib/kpiCalculations.ts` con todas las funciones de cálculo como funciones puras testeables.

- [x] 1.1 Crear `ui/src/lib/kpiCalculations.ts` con las interfaces `Robot`, `Job`, `Queue`, `Alert` y la función `parseDurationToSeconds` (helper interno)
  - _Requirements: 13.1_

- [x] 1.2 Implementar `calculateSuccessRate(jobs: Job[]): number | null`
  - Retorna `null` cuando `jobs.length === 0`
  - Retorna `Math.round(successJobs / total * 100)` donde success = `status.value === 'Success'`
  - _Requirements: 3.1, 3.2, 13.2_

- [x] 1.3 Implementar `calculateFleetAvailability(robots: Robot[]): number | null`
  - Retorna `null` cuando `robots.length === 0`
  - Available = robots con status en `['Online', 'Idle', 'Busy']`
  - _Requirements: 7.1, 7.2, 13.3_

- [x] 1.4 Implementar `calculateRobotUtilization(robots: Robot[]): { rate: number | null; busyCount: number; totalCount: number }`
  - Busy = robots con `status === 'Busy'`
  - `rate` es `null` cuando `totalCount === 0`
  - _Requirements: 6.1, 6.2, 13.4_

- [x] 1.5 Implementar `calculateQueueBacklog(queues: Queue[]): number`
  - Suma de `pendingItems` de todas las colas; retorna `0` si array vacío
  - _Requirements: 8.1, 8.2, 13.5_

- [x] 1.6 Implementar `calculateMtta(alerts: Alert[]): number | null`
  - Solo considera alertas con `acknowledgedAtUtc != null`
  - Retorna `null` si ninguna alerta tiene `acknowledgedAtUtc`
  - _Requirements: 10.1, 10.2, 10.3, 13.6_

- [x] 1.7 Implementar `calculateAvgCycleTime(jobs: Job[]): number | null`
  - Solo jobs en estados terminales (`Success`, `Failed`, `Stopped`, `Cancelled`) con `duration` no nulo
  - Retorna duración promedio en segundos usando `parseDurationToSeconds`
  - _Requirements: 5.1, 5.2, 5.3, 13.7_

- [x] 1.8 Implementar `calculateExceptionBreakdown(jobs: Job[]): { businessExceptions, systemExceptions, other, total }`
  - `total` = jobs con `status.value === 'Failed'`
  - `other` = failed jobs sin `errorType` conocido
  - _Requirements: 9.1, 9.2, 13.8_

- [x] 1.9 Implementar funciones de formato: `formatMtta(minutes: number | null): string`, `formatAvgCycleTime(seconds: number | null): string`, `getPercentageColor(value: number | null): string`
  - `getPercentageColor`: `text-success` ≥90%, `text-warning` 70-89%, `text-error` <70%, `text-gray-400` para null
  - `formatMtta`: `"—"` si null, `"Xm"` si <60, `"Xh YYm"` si ≥60
  - `formatAvgCycleTime`: misma lógica que `formatDuration` existente pero recibe segundos
  - _Requirements: 3.3, 3.4, 3.5, 7.3, 7.4, 7.5, 10.4, 13.9_

---

## Tarea 2: Crear componente KpiCard

Crear el componente reutilizable `ui/src/components/KpiCard.tsx`.

- [x] 2.1 Crear `ui/src/components/KpiCard.tsx` con la interfaz `KpiCardProps` exportada
  - Props: `label`, `value`, `subtitle`, `icon`, `iconColor`, `valueColor?`, `href?`, `pulse?`, `trend?`
  - _Requirements: 1.1_

- [x] 2.2 Implementar el cuerpo del card con icono, label, valor, subtitle y trend opcional
  - Valor `null` → muestra `"—"`
  - El icono se muestra en un `div` rounded con background `iconColor` al 20% de opacidad
  - Trend usa `TrendingUp` / `TrendingDown` de lucide-react
  - _Requirements: 1.2, 1.5, 1.6_

- [x] 2.3 Implementar el badge pulsante para alertas críticas
  - Solo visible cuando `pulse === true` AND `value > 0`
  - Usa clase CSS `animate-ping` de Tailwind
  - _Requirements: 1.4, 11.2_

- [x] 2.4 Implementar navegación clickable con `Link` de react-router-dom
  - Cuando `href` está definido, envuelve el card en `<Link to={href}>`
  - Aplica `hover:ring-1 hover:ring-accent/40 rounded-lg transition-all`
  - Cuando `href` no está, renderiza `<div>` normal
  - _Requirements: 1.3, 14.1, 14.2, 14.3, 14.4_

---

## Tarea 3: Reescribir Dashboard.tsx con 8 KPI Cards y 4 queries paralelas

Reemplazar el `Dashboard.tsx` actual con la versión que usa las 4 queries paralelas y muestra los 8 KPI cards.

- [x] 3.1 Añadir queries de `/queues` y `/alerts` en paralelo a las existentes de `/robots` y `/jobs`
  - `queryKey: ['queues-dashboard']` y `queryKey: ['alerts-dashboard']`
  - Actualizar `refetchAll` para incluir los 4 refetches
  - _Requirements: 2.4, 2.5, 2.6_

- [x] 3.2 Importar `KpiCard` y las funciones de `kpiCalculations.ts` en `Dashboard.tsx`
  - _Requirements: 2.1_

- [x] 3.3 Derivar todos los KPIs a partir de los datos de las 4 queries
  - `successRate`, `successRateColor`
  - `successCount`, `failedCount`, `stoppedCount`
  - `avgCycleTime`, `avgCycleTimeLabel`
  - `utilization` (rate, busyCount, totalCount)
  - `fleetAvail`, `fleetAvailColor`
  - `queueBacklog`
  - `excBreakdown`
  - `mttaMinutes`, `mttaLabel`
  - `criticalAlerts`, `unacknowledgedAll`
  - _Requirements: 3.1-3.6, 4.1-4.4, 5.1-5.5, 6.1-6.4, 7.1-7.7, 8.1-8.4, 9.1-9.5, 10.1-10.5, 11.1-11.5_

- [x] 3.4 Implementar Fila 1 de KPI cards: Success Rate, Jobs Volume, Fleet Availability, Critical Alerts
  - Grid `grid-cols-2 lg:grid-cols-4 gap-4`
  - Cada card usa el componente `<KpiCard />`
  - _Requirements: 2.1, 2.2, 2.3_

- [x] 3.5 Implementar Fila 2 de KPI cards: Avg Cycle Time, Robot Utilization, Queue Backlog, MTTA
  - Mismo grid que Fila 1
  - _Requirements: 2.1, 2.2, 2.3_

- [x] 3.6 Conservar las secciones existentes: Robot Fleet Grid y Recent Jobs Table
  - No modificar el layout ni la lógica de estas secciones, solo asegurarse de que siguen compilando con las nuevas interfaces
  - _Requirements: 2.6_

---

## Tarea 4: Añadir Exception Breakdown a Metrics.tsx

Añadir el gráfico donut de desglose de excepciones a la página de Metrics sin modificar los charts existentes.

- [x] 4.1 Añadir una nueva query `['metrics-jobs-breakdown']` en `Metrics.tsx` que llame a `/jobs?pageSize=50`
  - Acceder a `r.data?.items ?? []` ya que el endpoint devuelve paginación
  - `staleTime: 300_000`
  - _Requirements: 12.4_

- [x] 4.2 Calcular el breakdown usando `calculateExceptionBreakdown` de `kpiCalculations.ts`
  - Construir el array `donutData` filtrando segmentos con `value === 0`
  - _Requirements: 12.2, 12.3_

- [x] 4.3 Añadir el `PieChart` de Recharts como donut (innerRadius=50, outerRadius=80) DESPUÉS de los charts existentes
  - Importar `PieChart`, `Pie`, `Cell`, `Legend`, `Tooltip` desde `recharts`
  - Usar los colores: `#f5a623` (Business), `#f2495c` (System), `#6e7a86` (Otros)
  - Reutilizar `CHART_TOOLTIP_STYLE` existente
  - _Requirements: 12.1, 12.2, 12.3, 12.7_

- [x] 4.4 Implementar el estado vacío cuando no hay jobs fallidos
  - Mostrar card con mensaje `"Sin excepciones en el período"` cuando `breakdown.total === 0`
  - _Requirements: 12.5_

- [x] 4.5 Añadir leyenda con conteo y porcentaje por segmento
  - Usar el prop `formatter` de `<Legend>` para mostrar nombre + conteo + porcentaje
  - _Requirements: 12.6_

---

## Tarea 5: Verificar build de TypeScript

Asegurar que todos los cambios compilan sin errores.

- [x] 5.1 Ejecutar `npm run build` en `ui/` y corregir cualquier error de tipos
  - Verificar que las interfaces de `kpiCalculations.ts` son compatibles con los tipos usados en Dashboard.tsx y Metrics.tsx
  - _Requirements: NFR — build TypeScript sin errores_

- [x] 5.2 Verificar que no haya warnings de ESLint relacionados con variables no usadas (como el `void t` existente en Metrics.tsx)
  - _Requirements: NFR — código limpio_

---

## Tarea 6: Commit

- [x] 6.1 Hacer commit con mensaje: `feat: KPI Dashboard - 9 operational KPIs with real calculations`
  - Incluir todos los archivos: `kpiCalculations.ts`, `KpiCard.tsx`, `Dashboard.tsx`, `Metrics.tsx`

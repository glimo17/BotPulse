# Design — Bot Launcher

## Overview

Vista React en `/launcher` que permite a operadores lanzar procesos RPA unattended mediante un formulario. Usa las APIs existentes `/processes`, `/robots` y `POST /jobs`.

---

## Estructura Frontend

### Ruta y navegación

- Ruta: `/launcher`
- Entrada en sidebar: icono `Rocket` de lucide-react, label "Lanzar Bot"
- Requiere rol Operator o Administrator (guard de ruta existente)

### Componente principal: `ui/src/pages/Launcher.tsx`

```tsx
// Queries
const { data: processes } = useQuery(['processes'], () => api.get('/processes'))
const { data: robots }    = useQuery(['robots'], () => api.get('/robots'))

// Estado local
const [selectedProcess, setSelectedProcess] = useState<string | null>(null)
const [selectedRobot, setSelectedRobot]     = useState<string | null>(null) // null = Automático
const [params, setParams]                   = useState<Record<string, unknown>>({})
const [recentJobs, setRecentJobs]           = useState<LaunchedJob[]>([])

// Mutation: lanzar job
const launchMutation = useMutation({
  mutationFn: () => api.post('/jobs', {
    processExternalId: selectedProcess,
    robotExternalId: selectedRobot ?? undefined,
    parameters: params,
    priority: 'Normal',
  }),
  onSuccess: (res) => {
    toast.success(`Job lanzado: ${res.data.jobExternalId}`)
    setRecentJobs(prev => [{ id: res.data.jobExternalId, processId: selectedProcess!, status: 'Running', startedAt: new Date() }, ...prev].slice(0, 5))
  },
  onError: (err) => toast.error(`Error al lanzar: ${err.message}`)
})
```

### Componente ProcessSelector

- Dropdown con búsqueda (usa `<input>` + filter)
- Solo muestra procesos con `publicationStatus === 'Published'`
- Al seleccionar, dispara query `['process-params', id]` para cargar parámetros

### Componente RobotSelector

- Dropdown con badge de status por robot
- Primera opción: "Automático (sistema elige)" con value=null
- Robots Offline se muestran con warning badge pero son seleccionables

### Componente ParameterForm

```typescript
interface ProcessParamField {
  name: string
  type: 'String' | 'Int32' | 'Boolean' | 'DateTime'
  isRequired: boolean
  defaultValue?: string
}
```

Renderiza según `type`:
- String → `<input type="text">`
- Int32 → `<input type="number">`
- Boolean → `<select>` con True/False
- DateTime → `<input type="datetime-local">`

### Panel Recent Jobs

- Lista `recentJobs` en estado local (sesión)
- Auto-refresh: `useQuery(['job-status', id])` cada 10s por cada job en Running
- Muestra: ID (copyable), proceso, robot, status badge, tiempo transcurrido

---

## Endpoint API: POST /api/v1/jobs

Reutiliza el endpoint existente del `JobsController`. El request body es `StartJobRequest`:

```json
{
  "processExternalId": "proc-01",
  "robotExternalId": "robot-03",
  "parameters": { "BatchSize": 50 },
  "priority": "Normal"
}
```

Response `201 Created`:
```json
{ "jobExternalId": "job-sim-abc123" }
```

No se necesitan cambios en el backend para esta feature.

---

## Validación

- Proceso requerido: si no hay proceso seleccionado, botón Launch deshabilitado
- Parámetros requeridos: validación client-side antes del submit
- El botón muestra spinner con `launchMutation.isPending`

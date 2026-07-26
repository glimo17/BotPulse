# Requirements — DemoProvider

## Contexto

BotPulse es una plataforma vendor-agnostic de operaciones RPA. El único proveedor real actualmente disponible es `BotPulse.Providers.UiPath`, que requiere credenciales OAuth2 externas. El DemoProvider proporciona datos realistas en memoria para desarrollo local, demos, pruebas de UI y CI sin dependencias externas.

---

## 1. Activación por configuración

**REQ-1.1**
THE **sistema** SHALL soportar la activación del DemoProvider mediante la variable de configuración `RpaProvider=Demo` (clave de .NET configuration, mapeada desde `RPA_PROVIDER` en variables de entorno).

**REQ-1.2**
WHEN `RpaProvider` no está definido en la configuración, THE **sistema** SHALL activar el DemoProvider como proveedor por defecto (development convenience).

**REQ-1.3**
WHEN `RpaProvider=UiPath`, THE **sistema** SHALL activar el UiPath provider y no registrar el DemoProvider.

**REQ-1.4**
IF `RpaProvider=Demo`, THEN THE **startup** SHALL NOT requerir las claves `UiPath__ClientId`, `UiPath__ClientSecret`, `UiPath__BaseUrl` ni `UiPath__FolderId`. La ausencia de estas claves no producirá errores de validación.

---

## 2. Cumplimiento de contratos de interfaces

**REQ-2.1**
THE **DemoProvider** SHALL implementar las 7 interfaces granulares de proveedor definidas en `BotPulse.Core`:
- `IRobotProvider`
- `IJobProvider`
- `IQueueProvider`
- `ILogProvider`
- `IAssetProvider`
- `IMachineProvider`
- `IProcessProvider`

**REQ-2.2**
THE **DemoProvider** SHALL ser registrado en el contenedor DI con los mismos lifetimes que el UiPath provider: `DemoDataSeed` como singleton, y los 7 providers como scoped.

**REQ-2.3**
THE **DemoProvider** SHALL implementar todos los métodos de cada interfaz sin lanzar `NotImplementedException`. Todos los métodos deben retornar datos consistentes y válidos.

---

## 3. Realismo de los datos

**REQ-3.1**
THE **DemoDataSeed** SHALL generar, al iniciarse, el siguiente conjunto fijo de entidades:

| Entidad | Cantidad | Detalle |
|---|---|---|
| Robots | 6 | ROBOT-PROD-01 a ROBOT-PROD-06; 3 Idle, 2 Online, 1 Offline |
| Máquinas | 3 | SRV-BOT-01, SRV-BOT-02, SRV-BOT-03 |
| Procesos | 4 | AP_InvoiceProcessing, HR_OnboardingAutomation, FIN_ReconciliationBot, IT_TicketRouter |
| Jobs | ~80 | Distribuidos en las últimas 48 horas |
| Colas | 3 | FacturasEntrada, PagosRecurrentes, SolicitudesHR |
| Assets | 5 | Mix de tipos Credential y Text |
| Logs | 8–15 por job | Mensajes temáticos por proceso |

**REQ-3.2**
THE **datos de robots** SHALL incluir: `ExternalId` único (`"robot-01"` a `"robot-06"`), `Name` legible, `Status` variado (3 × `"Idle"`, 2 × `"Online"`, 1 × `"Offline"`), `MachineExternalId` apuntando a una máquina existente (robots 01–02 → `"machine-01"`, robots 03–04 → `"machine-02"`, robots 05–06 → `"machine-03"`), `LicenseType` como `"Attended"` o `"Unattended"`, y `LastHeartbeatUtc` dentro de los últimos 5 minutos para robots Online/Idle y hace ≥ 2 horas para el Offline.

**REQ-3.3**
THE **datos de máquinas** SHALL incluir: `ExternalId` único (`"machine-01"` a `"machine-03"`), `Name` tipo servidor (`"SRV-BOT-01"` a `"SRV-BOT-03"`), `Status` `"Available"` para 01 y 02, `"Unavailable"` para 03, `LastHeartbeatUtc` reciente, y `ConnectedRobotCount` correcto según los robots asignados (2, 2, 2).

**REQ-3.4**
THE **datos de procesos** SHALL incluir versiones semver, estado `"Published"`, descripción en español, y `CompatibleRobotCount` entre 2 y 6. Los procesos son:
- `"proc-01"` → `AP_InvoiceProcessing` v2.3.1 — Procesa facturas de proveedores automáticamente
- `"proc-02"` → `HR_OnboardingAutomation` v1.5.0 — Automatiza el onboarding de nuevos empleados
- `"proc-03"` → `FIN_ReconciliationBot` v3.1.2 — Reconcilia transacciones financieras diarias
- `"proc-04"` → `IT_TicketRouter` v1.0.4 — Clasifica y asigna tickets de soporte automáticamente

**REQ-3.5**
THE **distribución de jobs** SHALL seguir la proporción: 72% `"Success"`, 18% `"Failed"`, 7% `"Stopped"`, 3% `"Running"`. Los ~80 jobs estarán distribuidos a lo largo de las últimas 48 horas con densidad variable (más actividad en horario laboral simulado).

**REQ-3.6**
IF un job tiene `Status == "Failed"`, THEN THE **DemoDataSeed** SHALL asignarle `ErrorType` como `"BusinessException"` o `"SystemException"` con distribución 50/50, y un `ErrorMessage` descriptivo acorde al proceso.

**REQ-3.7**
THE **datos de colas** SHALL incluir:
- `"queue-01"` → `FacturasEntrada`: entre 15 y 40 `PendingItems`, `ProcessedItems` ≥ 500, `FailedItems` ≈ 12% de procesados
- `"queue-02"` → `PagosRecurrentes`: entre 3 y 8 `PendingItems`, `ProcessedItems` ≥ 200, `FailedItems` ≈ 3% de procesados
- `"queue-03"` → `SolicitudesHR`: entre 0 y 5 `PendingItems`, `ProcessedItems` ≥ 80, `FailedItems` ≈ 5% de procesados
- `TotalItems` SHALL ser la suma de `ProcessedItems + FailedItems + PendingItems`

**REQ-3.8**
THE **datos de assets** SHALL incluir 5 assets con `ExternalId` único (`"asset-01"` a `"asset-05"`), `Type` siendo `"Credential"` para 2 de ellos y `"Text"` para los 3 restantes, `Scope` `"Global"` o `"Robot"`, y `LastModifiedUtc` en los últimos 30 días.

**REQ-3.9**
THE **logs de ejecución** SHALL incluir entre 8 y 15 entradas por job. Cada entrada tendrá `Severity` variado (`"Info"`, `"Warning"`, `"Error"`), `LoggerName` acorde al proceso, y mensajes con terminología de negocio realista.

---

## 4. Almacenamiento en memoria

**REQ-4.1**
THE **DemoDataSeed** SHALL mantener todos los datos en memoria como singleton, sin realizar llamadas HTTP externas, sin acceder a PostgreSQL y sin leer archivos en disco.

**REQ-4.2**
THE **DemoDataSeed** SHALL usar colecciones thread-safe (mediante `lock` sobre listas privadas o `ConcurrentDictionary`) para garantizar consistencia ante accesos concurrentes desde múltiples requests HTTP simultáneos.

**REQ-4.3**
THE **DemoDataSeed** SHALL ser el único componente con estado mutable en el DemoProvider. Los 7 providers leen del seed y no mantienen estado propio.

---

## 5. Simulación de cambios en tiempo real

**REQ-5.1**
THE **DemoDataSeed** SHALL activar un `System.Threading.Timer` al construirse, con periodo de 30 segundos, que ejecuta las siguientes mutaciones:

**REQ-5.2**
WHEN el timer dispara, THE **DemoDataSeed** SHALL rotar exactamente 1 robot de estado `"Idle"` a `"Busy"` (o de `"Busy"` a `"Idle"` si no hay Idle disponibles), actualizando su `LastHeartbeatUtc`.

**REQ-5.3**
WHEN el timer dispara, THE **DemoDataSeed** SHALL completar todos los jobs con `Status == "Running"` cuya `StartTimeUtc` sea más de 2 minutos anterior al momento actual, cambiando su estado a `"Success"` o `"Failed"` (90%/10%) y asignando `EndTimeUtc` y `Duration`.

**REQ-5.4**
WHEN el timer dispara, THE **DemoDataSeed** SHALL ajustar los `PendingItems` de cada cola en ±(0 a 3) ítems (número aleatorio en ese rango), sin permitir que `PendingItems` sea negativo. `TotalItems` SHALL actualizarse consistentemente.

**REQ-5.5**
WHEN el timer dispara, THE **DemoDataSeed** SHALL crear 0 o 1 nuevo job con `Status == "Running"` (probabilidad 50%), asignando aleatoriamente proceso y robot disponibles, con `StartTimeUtc = DateTime.UtcNow`.

**REQ-5.6**
THE **DemoDataSeed** SHALL implementar `IDisposable` y disponer el timer en `Dispose()`.

---

## 6. Health check

**REQ-6.1**
WHEN `RpaProvider=Demo`, THE **health check** `rpa-provider` SHALL reportar siempre `HealthCheckResult.Healthy("Demo provider active")` sin realizar ninguna comprobación de conectividad externa.

**REQ-6.2**
THE **DemoProviderRegistration** SHALL registrar un health check dedicado (`DemoProviderHealthCheck`) que se añade al `IHealthChecksBuilder` con el tag `"ready"`, de modo que `/health/ready` sea funcional sin base de datos de RPA.

---

## 7. Operaciones de escritura (JobProvider)

**REQ-7.1**
WHEN se invoca `StartJobAsync(request)`, THE **DemoJobProvider** SHALL crear un nuevo `JobSnapshot` con `Status == "Running"`, `StartTimeUtc = DateTime.UtcNow`, `ExternalId` generado como GUID string, y lo SHALL agregar a la colección en memoria del `DemoDataSeed`. El método SHALL retornar un `StartJobResult` con ese `ExternalId`.

**REQ-7.2**
IF `request.ProcessExternalId` no corresponde a ningún proceso conocido en el seed, THEN THE **DemoJobProvider** SHALL lanzar `InvalidOperationException` con mensaje descriptivo.

**REQ-7.3**
WHEN se invoca `StopJobAsync(externalId)`, THE **DemoJobProvider** SHALL buscar el job en memoria y, si existe con `Status == "Running"`, cambiar su estado a `"Stopped"` y asignar `EndTimeUtc = DateTime.UtcNow`. Si el job no existe o no está en Running, el método SHALL completar sin error (idempotente).

**REQ-7.4**
WHEN se invoca `CancelJobAsync(externalId)`, THE **DemoJobProvider** SHALL buscar el job en memoria y, si existe con `Status == "Running"`, cambiar su estado a `"Cancelled"` y asignar `EndTimeUtc = DateTime.UtcNow`. Si el job no existe o no está en Running, el método SHALL completar sin error (idempotente).

**REQ-7.5**
THE **DemoJobProvider** `GetJobsAsync(query)` SHALL aplicar los filtros de `JobQuery`: `UpdatedSinceUtc` (filtra por `StartTimeUtc` o `EndTimeUtc`), `Status`, `RobotExternalId`, `ProcessExternalId`, con paginación mediante `Skip` y `Top`. IF `query.Top == 0`, THEN SHALL retornar todos los resultados sin truncar.

---

## 8. Filtrado en providers de lectura

**REQ-8.1**
THE **DemoQueueProvider** `GetQueueItemsAsync(query)` SHALL filtrar por `QueueName` si está presente, y aplicar `Top` como límite de resultados.

**REQ-8.2**
THE **DemoLogProvider** `GetExecutionLogsAsync(query)` SHALL filtrar por `JobExternalId`, `FromUtc`, `ToUtc` si están presentes, y aplicar `Top` como límite.

---

## 9. Consistencia de datos de retorno

**REQ-9.1**
THE **DemoRobotProvider** `GetRobotByIdAsync(externalId)` SHALL retornar `null` si el `externalId` no existe en el seed, sin lanzar excepción.

**REQ-9.2**
THE **DemoMachineProvider** `GetMachineByIdAsync(externalId)` SHALL retornar `null` si el `externalId` no existe en el seed, sin lanzar excepción.

**REQ-9.3**
THE **DemoProcessProvider** `GetProcessParametersAsync(processExternalId)` SHALL retornar una lista no vacía de `ProcessParameter` para cada proceso conocido, y lista vacía para IDs desconocidos.

---

## 10. Configuración de entorno

**REQ-10.1**
THE **`.env.example`** SHALL incluir la clave `RPA_PROVIDER=Demo` como valor por defecto documentado, con comentario explicando que `UiPath` requiere las claves `UiPath__*`.

**REQ-10.2**
THE **`docker-compose.yml`** no SHALL requerir modificaciones adicionales para funcionar con `RPA_PROVIDER=Demo`, dado que ya mapea variables de entorno al contenedor.

---

## Glosario

| Término | Definición |
|---|---|
| `DemoDataSeed` | Singleton que genera y mantiene en memoria todos los datos del DemoProvider |
| `RpaProvider` | Clave de configuración .NET que determina qué provider RPA se activa |
| `ExternalId` | Identificador string único que el provider asigna a cada entidad |
| `JobSnapshot` | Registro inmutable del estado de un job en un momento dado |
| `StartJobResult` | DTO retornado al iniciar un job, contiene el `JobExternalId` generado |

# Requisitos — Bot Launcher (BotPulse)

## Introducción

El módulo Bot Launcher permite a los operadores y administradores ejecutar procesos RPA unattended directamente desde BotPulse mediante un botón, sin necesidad de acceder al Orchestrator externo. El objetivo es reducir la fricción operacional para lanzamientos ad-hoc y de emergencia.

---

## Sección 1: Vista de Lanzamiento

### Requisito 1: Acceso al Bot Launcher

**Historia de Usuario:** Como operador, quiero una vista dedicada para lanzar bots, para ejecutar procesos sin salir de BotPulse.

#### Criterios de Aceptación

1. THE **BotPulse Navigation** SHALL include a "Lanzar Bot" entry accessible from the main sidebar.
2. THE **Bot Launcher View** SHALL be accessible at route `/launcher`.
3. THE **Bot Launcher View** SHALL require at least the Operator role. Viewer users SHALL see HTTP 403.

---

### Requisito 2: Selección de Proceso

**Historia de Usuario:** Como operador, quiero seleccionar el proceso a ejecutar desde una lista, para no tener que recordar IDs externos.

#### Criterios de Aceptación

1. THE **Process Selector** SHALL load the list of processes from `IProcessProvider.GetProcessesAsync()` (read on-demand, same as /processes view).
2. THE **Process Selector** SHALL display process name, version and publication status.
3. THE **Process Selector** SHALL support search by process name.
4. WHEN a process is selected, THE **Bot Launcher View** SHALL show its input parameters fetched from `IProcessProvider.GetProcessParametersAsync(processId)`.
5. THE **Process Selector** SHALL only show processes with `PublicationStatus == "Published"`.

---

### Requisito 3: Selección de Robot

**Historia de Usuario:** Como operador, quiero seleccionar el robot que ejecutará el proceso, o dejar que el sistema elija automáticamente.

#### Criterios de Aceptación

1. THE **Robot Selector** SHALL load available robots from `IRobotProvider.GetRobotsAsync()`.
2. THE **Robot Selector** SHALL offer an "Automático" option (no specific robot, let the Orchestrator decide).
3. THE **Robot Selector** SHALL show each robot's current status badge (Idle/Online/Busy/Offline).
4. WHEN a robot with status "Offline" is selected, THE **Bot Launcher View** SHALL show a warning but SHALL NOT block submission.
5. THE **Robot Selector** SHALL default to "Automático".

---

### Requisito 4: Parámetros de Entrada

**Historia de Usuario:** Como operador, quiero pasar parámetros al proceso si los requiere, para ejecutarlo con los datos correctos.

#### Criterios de Aceptación

1. WHEN a process has input parameters, THE **Bot Launcher View** SHALL render a form field for each parameter.
2. THE **Parameter Form** SHALL mark required parameters with a visual indicator.
3. THE **Parameter Form** SHALL show the default value (if any) as placeholder.
4. WHEN a required parameter is empty on submit, THE **Bot Launcher View** SHALL show a validation error and SHALL NOT submit.
5. THE **Parameter Form** SHALL support parameter types: String, Int32, Boolean, DateTime.

---

### Requisito 5: Ejecución con Botón

**Historia de Usuario:** Como operador, quiero lanzar el proceso con un botón y ver confirmación inmediata, para saber que el comando fue aceptado.

#### Criterios de Aceptación

1. THE **Launch Button** SHALL call `POST /api/v1/jobs` (which invokes `IJobProvider.StartJobAsync`) when clicked.
2. WHEN the launch is successful, THE **Bot Launcher View** SHALL show a toast notification with the new Job ID and a link to view the job.
3. WHEN the launch fails, THE **Bot Launcher View** SHALL show an error message with the reason returned by the API.
4. THE **Launch Button** SHALL be disabled and show a spinner while the request is in flight.
5. WHEN the launch succeeds, THE **Bot Launcher View** SHALL NOT reset the form automatically, allowing the operator to launch the same process again with different parameters.

---

### Requisito 6: Seguimiento Post-Lanzamiento

**Historia de Usuario:** Como operador, quiero ver el estado del job recién lanzado sin navegar a otra vista.

#### Criterios de Aceptación

1. AFTER a successful launch, THE **Bot Launcher View** SHALL display a "Job Recientes" panel showing the last 5 jobs launched in the current session.
2. THE **Recent Jobs Panel** SHALL show job ID, process name, robot, status and elapsed time.
3. THE **Recent Jobs Panel** SHALL auto-refresh every 10 seconds while the view is open.
4. WHEN a job in the panel transitions to Success or Failed, THE **Recent Jobs Panel** SHALL highlight the status change.

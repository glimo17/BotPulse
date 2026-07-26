# Tasks — Bot Launcher

- [ ] 1. Crear página `ui/src/pages/Launcher.tsx`
  - Setup de queries: `/processes` y `/robots`
  - Estado local: selectedProcess, selectedRobot, params, recentJobs
  - Mutation `useMutation` para `POST /api/v1/jobs`
  - Layout: ProcessSelector | RobotSelector | ParameterForm | LaunchButton
  **Acceptance criteria:** La página renderiza sin errores de TypeScript.

- [ ] 2. Implementar `ProcessSelector` como componente interno
  - Filtro por `publicationStatus === 'Published'`
  - Búsqueda por nombre
  - Al seleccionar, carga parámetros con query `['process-params', id]`
  **Acceptance criteria:** Al seleccionar un proceso, aparece el ParameterForm.

- [ ] 3. Implementar `RobotSelector` como componente interno
  - Primera opción: "Automático"
  - Badge de status en cada robot
  - Warning visual si se selecciona robot Offline
  **Acceptance criteria:** El selector muestra todos los robots con su status.

- [ ] 4. Implementar `ParameterForm` para tipos String, Int32, Boolean, DateTime
  - Campos requeridos marcados con asterisco
  - Validación: campos requeridos vacíos bloquean el submit
  **Acceptance criteria:** Submit con campo requerido vacío muestra error.

- [ ] 5. Implementar LaunchButton con estado de loading
  - Deshabilitado cuando no hay proceso seleccionado o la mutation está en curso
  - Spinner durante `launchMutation.isPending`
  - Toast de éxito con jobId / toast de error con mensaje
  **Acceptance criteria:** El botón lanza el job y muestra toast con el jobId.

- [ ] 6. Implementar panel Recent Jobs con auto-refresh
  - Lista últimos 5 jobs lanzados en la sesión
  - Poll de status cada 10s para jobs en Running
  **Acceptance criteria:** El panel actualiza el status de jobs Running cada 10s.

- [ ] 7. Registrar ruta `/launcher` en el router y añadir entrada en sidebar
  - Icono `Rocket` en sidebar entre Jobs y Queues
  - Proteger ruta con guard de rol Operator+
  **Acceptance criteria:** La ruta `/launcher` es accesible desde el sidebar.

- [ ] 8. Verificar build TypeScript: `npm run build`
  **Acceptance criteria:** Build pasa sin errores.

- [ ] 9. Commit: `feat: Bot Launcher - one-click unattended bot execution`

📋 Sub-plan de Tareas Frontend (Fase 10)
Bloque 1: Esencial para Pruebas (Objetivo de hoy)
10.1 Setup y Estructura Base:

Configurar Vite + React + TypeScript en ui/ con Tailwind CSS y proxy de desarrollo apuntando a la API.

10.2 Autenticación y Seguridad:

Implementar pantalla de Login (POST /api/v1/auth/login) guardando el token JWT en memoria (context provider).

Configurar interceptor de Axios para inyectar el header Authorization: Bearer <token> y redirección automática a /login ante un error 401.

10.3 Layout y Navegación Principal:

Crear un AppLayout limpio con Sidebar y Header (usuario + logout) enfocado en modo oscuro por defecto para reducir fatiga visual.

10.4 Dashboard Mínimo y Vista Clave:

Implementar tarjetas de resumen (KPIs) y una tabla funcional (preferiblemente la de Jobs o Robots) consumiendo los endpoints reales de la API v1.

Añadir el cliente SSE (EventSource) conectado a /api/v1/notifications/stream para refrescar los datos automáticamente cuando ocurran eventos en el backend.

Bloque 2: Pulido y Características Avanzadas (Para mañana o si avanzamos rápido hoy)
10.5 Vistas Completas:

Terminar las vistas de Queues y Alerts con su respectiva acción de Acknowledge (POST /api/v1/alerts/{id}/ack).

10.6 Experiencia de Usuario (UX) y Productividad:

Implementar búsqueda global rápida estilo Command Palette (Ctrl + K).

Añadir atajos de teclado básicos (R para refrescar, Esc para cerrar modales/drawers).

Auto-refresh visual con indicador de estado de conexión y copiado rápido de IDs con un clic (One-Click Copy).

Cambios dinámicos en el título de la pestaña del navegador si entra una alerta crítica vía SSE (⚠️ (X) BotPulse - Alerta Crítica).

📱 Directrices de Diseño, Apariencia y Responsive
Dirección Visual (Theming):

Mantener un Modo Oscuro nativo por defecto (tonos grises pizarra profundos en fondos y tarjetas, con acentos en azul técnico o esmeralda).

Usar Badges de tipo "Pill" con colores estrictos para estados (Verde para Success/Online, Azul/Morado con pulso para Running/Busy, Amarillo para Pending, Rojo para Failed/Offline).

Enfoque Responsive Simple y Eficiente:

Tablas a Cards móviles: En pantallas pequeñas, transformar las filas de las tablas densas en tarjetas apiladas (stacked cards) con la información crítica arriba y los detalles secundarios abajo para evitar el scroll horizontal.

Navegación adaptable: Usar un menú colapsable o barra compacta en dispositivos móviles para aprovechar el espacio de monitoreo.

Filtros rápidos: Ocultar los filtros avanzados de fecha/estado dentro de un cajón desplegable (drawer) flotante en pantallas pequeñas.
✨ Features y Funcionalidades "Simples pero Potentes" (Que enamoran)
1. Modo Compacto vs. Modo Cómodo (Density Toggle):
Añade un simple botón en la cabecera para alternar entre una vista espaciosa y una vista ultra-compacta (densidad de datos). A los ingenieros les encanta poder ver el doble de filas en su monitor sin hacer scroll.

2. Búsqueda Global Estilo "Command Palette" (Ctrl + K):
Una barra de búsqueda centralizada que se abra con un atajo de teclado. Que el operador escriba "Finanzas" y le sugiera instantáneamente saltar al robot, proceso o job relacionado con esa palabra. Es un feature moderno que da una experiencia de software "premium" con muy poco esfuerzo visual.

3. Auto-Refresh Visual con Cuenta Regresiva:
Un pequeño indicador circular o texto sutil que muestre "Actualizando en 15s..." junto a un botón para pausar o forzar el refresco. Da mucha tranquilidad operativa saber que la pantalla está viva sin tener que recargar todo el navegador.

4. Copiado Rápido con Feedback Visual (One-Click Copy):
Los IDs de los jobs, los nombres de los errores o los hashes largos son molestos de copiar. Hacer que al hacer clic en cualquier ID o external ID se copie automáticamente al portapapeles y muestre un mini tooltip "¡Copiado!" es un detalle que ahorra micro-fricciones constantes.

5. Indicador de Alertas No Atendidas en la Pestaña del Navegador:
Si entra una alerta crítica vía SSE mientras el operador tiene otra pestaña abierta (como el correo o documentación), cambiar dinámicamente el título de la pestaña del navegador a algo como ⚠️ (2) BotPulse - Alerta Crítica asegura que nunca se pierda un incidente.
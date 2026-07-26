using System.Globalization;
using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Providers.Demo;

/// <summary>
/// Singleton that generates and maintains all demo RPA data in memory.
/// Simulates real-time changes via a 30-second Timer.
/// </summary>
public sealed class DemoDataSeed : IDisposable
{
    private readonly object _lock = new();
    private readonly List<RobotSnapshot> _robots;
    private readonly List<MachineSnapshot> _machines;
    private readonly List<ProcessSnapshot> _processes;
    private readonly List<JobSnapshot> _jobs;
    private readonly List<QueueSnapshot> _queues;
    private readonly List<QueueItemSnapshot> _queueItems;
    private readonly List<ExecutionLogSnapshot> _logs;
    private readonly List<AssetMetadata> _assets;
    private readonly Dictionary<string, List<ProcessParameter>> _processParams;
    private readonly Timer _timer;
    private readonly Random _rng = new(42);

    private static readonly string[] Departments = { "Ingeniería", "RRHH", "Finanzas", "IT" };
    private static readonly string[] Categories = { "Hardware", "Software", "Red", "Acceso" };
    private static readonly string[] Teams = { "Soporte N1", "Infraestructura", "Desarrollo", "Seguridad" };

    private static readonly string[] LoggerNames =
    {
        "AP.InvoiceProcessing",
        "HR.Onboarding",
        "FIN.Reconciliation",
        "IT.TicketRouter"
    };

    public DemoDataSeed()
    {
        _robots = SeedRobots();
        _machines = SeedMachines();
        _processes = SeedProcesses();
        _processParams = SeedProcessParameters();
        _jobs = SeedJobs();
        _queues = SeedQueues();
        _queueItems = SeedQueueItems();
        _assets = SeedAssets();
        _logs = SeedLogs();

        _timer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    #region Seed Methods

    private static List<RobotSnapshot> SeedRobots()
    {
        var now = DateTime.UtcNow;
        return new List<RobotSnapshot>
        {
            new("robot-01", "ROBOT-PROD-01", "Idle", "machine-01", "Unattended", now.AddMinutes(-1)),
            new("robot-02", "ROBOT-PROD-02", "Idle", "machine-01", "Unattended", now.AddMinutes(-2)),
            new("robot-03", "ROBOT-PROD-03", "Idle", "machine-02", "Unattended", now.AddMinutes(-3)),
            new("robot-04", "ROBOT-PROD-04", "Online", "machine-02", "Attended", now.AddSeconds(-30)),
            new("robot-05", "ROBOT-PROD-05", "Online", "machine-03", "Attended", now.AddSeconds(-45)),
            new("robot-06", "ROBOT-PROD-06", "Offline", "machine-03", "Unattended", now.AddHours(-3)),
        };
    }

    private static List<MachineSnapshot> SeedMachines()
    {
        var now = DateTime.UtcNow;
        return new List<MachineSnapshot>
        {
            new("machine-01", "SRV-BOT-01", "Available", now.AddMinutes(-1), 2),
            new("machine-02", "SRV-BOT-02", "Available", now.AddMinutes(-2), 2),
            new("machine-03", "SRV-BOT-03", "Unavailable", now.AddHours(-3), 2),
        };
    }

    private static List<ProcessSnapshot> SeedProcesses()
    {
        return new List<ProcessSnapshot>
        {
            new("proc-01", "AP_InvoiceProcessing", "2.3.1", "Published",
                "Procesa facturas de proveedores automáticamente desde el portal SAP", 6),
            new("proc-02", "HR_OnboardingAutomation", "1.5.0", "Published",
                "Automatiza el onboarding de nuevos empleados en RRHH y Active Directory", 4),
            new("proc-03", "FIN_ReconciliationBot", "3.1.2", "Published",
                "Reconcilia transacciones financieras diarias contra el libro mayor", 6),
            new("proc-04", "IT_TicketRouter", "1.0.4", "Published",
                "Clasifica y asigna tickets de soporte de ServiceNow automáticamente", 2),
        };
    }

    private static Dictionary<string, List<ProcessParameter>> SeedProcessParameters()
    {
        return new Dictionary<string, List<ProcessParameter>>
        {
            ["proc-01"] = new List<ProcessParameter>
            {
                new("InvoiceDateFrom", "DateTime", true, null),
                new("BatchSize", "Int32", false, "50"),
                new("DryRun", "Boolean", false, "false"),
            },
            ["proc-02"] = new List<ProcessParameter>
            {
                new("EmployeeId", "String", true, null),
                new("Department", "String", false, "General"),
            },
            ["proc-03"] = new List<ProcessParameter>
            {
                new("ReportDate", "DateTime", true, null),
                new("SendEmailReport", "Boolean", false, "true"),
            },
            ["proc-04"] = new List<ProcessParameter>
            {
                new("QueueName", "String", false, "TIER1"),
                new("MaxTickets", "Int32", false, "100"),
            },
        };
    }

    private List<JobSnapshot> SeedJobs()
    {
        var now = DateTime.UtcNow;
        var baseTime = now.AddHours(-48);
        var jobs = new List<JobSnapshot>(80);
        var robotIds = new[] { "robot-01", "robot-02", "robot-03", "robot-04", "robot-05" };
        var processIds = new[] { "proc-01", "proc-02", "proc-03", "proc-04" };

        for (int i = 0; i < 80; i++)
        {
            var processId = processIds[i % 4];
            var robotId = robotIds[i % 5];
            var machineId = GetMachineForRobot(robotId);
            var status = DetermineJobStatus(i);

            DateTime startTime;
            DateTime? endTime = null;
            TimeSpan? duration = null;
            string? errorType = null;
            string? errorMessage = null;

            if (status == "Running")
            {
                startTime = now.AddSeconds(-_rng.Next(30, 91));
            }
            else
            {
                var hoursAgo = _rng.NextDouble() * 48.0;
                startTime = baseTime.AddHours(hoursAgo);
                var durationMinutes = _rng.Next(1, 46);
                duration = TimeSpan.FromMinutes(durationMinutes);
                endTime = startTime + duration;
            }

            if (status == "Failed")
            {
                errorType = i % 2 == 0 ? "BusinessException" : "SystemException";
                errorMessage = GenerateErrorMessage(processId, errorType);
            }

            jobs.Add(new JobSnapshot(
                ExternalId: $"job-{i + 1:D3}",
                ProcessExternalId: processId,
                RobotExternalId: robotId,
                MachineExternalId: machineId,
                Status: status,
                StartTimeUtc: startTime,
                EndTimeUtc: endTime,
                Duration: duration,
                ErrorType: errorType,
                ErrorMessage: errorMessage));
        }

        return jobs;
    }

    private static string DetermineJobStatus(int index)
    {
        // For 80 jobs: 58 Success (72.5%), 14 Failed (17.5%), 6 Stopped (7.5%), 2 Running (2.5%)
        if (index < 58)
        {
            return "Success";
        }

        if (index < 72)
        {
            return "Failed";
        }

        if (index < 78)
        {
            return "Stopped";
        }

        return "Running";
    }

    private static string GetMachineForRobot(string robotId)
    {
        return robotId switch
        {
            "robot-01" or "robot-02" => "machine-01",
            "robot-03" or "robot-04" => "machine-02",
            "robot-05" => "machine-03",
            _ => "machine-01"
        };
    }

    private string GenerateErrorMessage(string processId, string errorType)
    {
        return (processId, errorType) switch
        {
            ("proc-01", "BusinessException") => $"Factura duplicada detectada: número INV-{_rng.Next(10000, 99999)} ya procesado",
            ("proc-01", "SystemException") => "Timeout al conectar con SAP BAPI BAPI_INCOMINGINVOICE_CREATE",
            ("proc-02", "BusinessException") => $"Empleado EMP-{_rng.Next(1000, 9999)} ya existe en Active Directory",
            ("proc-02", "SystemException") => "Error al crear buzón Exchange: servicio no disponible",
            ("proc-03", "BusinessException") => $"Diferencia de reconciliación supera umbral: EUR {_rng.Next(100, 9999)}.{_rng.Next(10, 99)}",
            ("proc-03", "SystemException") => "Fallo de conexión a base de datos contabilidad: timeout",
            ("proc-04", "BusinessException") => $"Ticket TKT-{_rng.Next(10000, 99999)} no tiene categoría asignable a ningún equipo",
            ("proc-04", "SystemException") => "API ServiceNow retornó 503 Service Unavailable",
            _ => "Error desconocido en ejecución"
        };
    }

    private static List<QueueSnapshot> SeedQueues()
    {
        return new List<QueueSnapshot>
        {
            new("queue-01", "FacturasEntrada", 722, 620, 74, 28),
            new("queue-02", "PagosRecurrentes", 257, 245, 7, 5),
            new("queue-03", "SolicitudesHR", 103, 95, 5, 3),
        };
    }

    private List<QueueItemSnapshot> SeedQueueItems()
    {
        var now = DateTime.UtcNow;
        var items = new List<QueueItemSnapshot>(30);
        var queueNames = new[] { "FacturasEntrada", "PagosRecurrentes", "SolicitudesHR" };
        var queueIds = new[] { "queue-01", "queue-02", "queue-03" };

        for (int q = 0; q < 3; q++)
        {
            for (int n = 1; n <= 10; n++)
            {
                string status;
                if (n <= 7)
                {
                    status = "Processed";
                }
                else if (n <= 9)
                {
                    status = "Failed";
                }
                else
                {
                    status = "InProgress";
                }

                int retryCount = status == "Failed" ? _rng.Next(1, 4) : 0;
                var processingStart = now.AddMinutes(-_rng.Next(10, 120));
                DateTime? processingEnd = status == "InProgress"
                    ? null
                    : processingStart.AddSeconds(_rng.Next(5, 300));
                string? originalExternalItemId = retryCount > 0
                    ? $"qi-{queueIds[q]}-orig-{n:D2}"
                    : null;
                string outputJson = status == "Processed" ? "{\"result\":\"ok\"}" : "{}";

                items.Add(new QueueItemSnapshot(
                    ExternalItemId: $"qi-{queueIds[q]}-{n:D2}",
                    QueueName: queueNames[q],
                    Status: status,
                    RetryCount: retryCount,
                    ProcessingStartUtc: processingStart,
                    ProcessingEndUtc: processingEnd,
                    OriginalExternalItemId: originalExternalItemId,
                    OutputMetadataJson: outputJson));
            }
        }

        return items;
    }

    private static List<AssetMetadata> SeedAssets()
    {
        var now = DateTime.UtcNow;
        return new List<AssetMetadata>
        {
            new("asset-01", "SAP_ServiceAccount", "Credential", "Global", now.AddDays(-5)),
            new("asset-02", "Exchange_Credentials", "Credential", "Robot", now.AddDays(-10)),
            new("asset-03", "SAP_BaseUrl", "Text", "Global", now.AddDays(-30)),
            new("asset-04", "HR_SystemEndpoint", "Text", "Global", now.AddDays(-15)),
            new("asset-05", "AlertEmail_Recipients", "Text", "Global", now.AddDays(-2)),
        };
    }

    private List<ExecutionLogSnapshot> SeedLogs()
    {
        var logs = new List<ExecutionLogSnapshot>();

        foreach (var job in _jobs)
        {
            logs.AddRange(GenerateJobLogs(job));
        }

        return logs;
    }

    private List<ExecutionLogSnapshot> GenerateJobLogs(JobSnapshot job)
    {
        var logCount = _rng.Next(8, 16);
        var logs = new List<ExecutionLogSnapshot>(logCount);
        var processIndex = GetProcessIndex(job.ProcessExternalId);
        var loggerName = LoggerNames[processIndex];
        var templates = GetLogTemplates(processIndex);

        var jobStart = job.StartTimeUtc;
        var jobEnd = job.EndTimeUtc ?? DateTime.UtcNow;
        var totalDuration = jobEnd - jobStart;
        if (totalDuration.TotalSeconds < 1)
        {
            totalDuration = TimeSpan.FromMinutes(2);
        }

        for (int i = 0; i < logCount; i++)
        {
            var progress = (double)i / (logCount - 1);
            var timestamp = jobStart.AddSeconds(totalDuration.TotalSeconds * progress);
            var templateIndex = i % templates.Length;
            var message = templates[templateIndex];
            var severity = "Info";

            if (job.Status == "Failed" && i >= logCount - 2)
            {
                if (i == logCount - 2)
                {
                    severity = "Warning";
                    message = GetWarningMessage(processIndex);
                }
                else
                {
                    severity = "Error";
                    message = job.ErrorMessage ?? "Error en ejecución";
                }
            }
            else if (job.Status == "Failed" && i == logCount - 3)
            {
                severity = "Warning";
                message = GetWarningMessage(processIndex);
            }

            logs.Add(new ExecutionLogSnapshot(
                JobExternalId: job.ExternalId,
                RobotExternalId: job.RobotExternalId,
                ProcessExternalId: job.ProcessExternalId,
                TimestampUtc: timestamp,
                Severity: severity,
                LoggerName: loggerName,
                Message: FillTemplate(message)));
        }

        return logs;
    }

    private static int GetProcessIndex(string processExternalId)
    {
        return processExternalId switch
        {
            "proc-01" => 0,
            "proc-02" => 1,
            "proc-03" => 2,
            "proc-04" => 3,
            _ => 0
        };
    }

    private static string[] GetLogTemplates(int processIndex)
    {
        return processIndex switch
        {
            0 => new[]
            {
                "Iniciando procesamiento de facturas pendientes",
                "Conectando a SAP sistema de cuentas por pagar",
                "Facturas encontradas en bandeja: {n}",
                "Procesando factura {id} de proveedor {vendor}",
                "Factura {id} validada exitosamente",
                "Importe total procesado: EUR {amount}",
                "Cerrando sesión SAP",
                "Procesamiento completado. Facturas: {ok} OK, {err} errores"
            },
            1 => new[]
            {
                "Iniciando proceso de onboarding para empleado {id}",
                "Creando cuenta de usuario en Active Directory",
                "Asignando grupos de seguridad según departamento {dept}",
                "Configurando buzón de correo Exchange",
                "Provisionando acceso a sistemas corporativos",
                "Enviando email de bienvenida a {email}",
                "Onboarding completado satisfactoriamente"
            },
            2 => new[]
            {
                "Iniciando reconciliación para fecha {date}",
                "Descargando extracto bancario: {n} transacciones",
                "Cargando libro mayor contable",
                "Comparando {n} registros",
                "Se encontraron {n} diferencias menores (< EUR 1)",
                "Diferencias dentro del umbral permitido",
                "Generando informe de reconciliación",
                "Reconciliación completada. Coincidencias: {pct}%"
            },
            3 => new[]
            {
                "Conectando a cola ServiceNow {queue}",
                "Tickets pendientes de clasificación: {n}",
                "Analizando ticket {id}: {title}",
                "Ticket {id} clasificado como {category}, asignando a {team}",
                "Enviando notificación al equipo {team}",
                "Tickets procesados: {n}"
            },
            _ => new[] { "Ejecutando paso de proceso" }
        };
    }

    private static string GetWarningMessage(int processIndex)
    {
        return processIndex switch
        {
            0 => "Advertencia: facturas requieren revisión manual",
            1 => "Advertencia: permisos parciales asignados",
            2 => "Se encontraron diferencias que superan el umbral",
            3 => "Tickets sin categoría detectados",
            _ => "Advertencia genérica"
        };
    }

    private string FillTemplate(string template)
    {
        var result = template;
        if (result.Contains("{n}"))
        {
            result = result.Replace("{n}", _rng.Next(5, 500).ToString(CultureInfo.InvariantCulture));
        }

        if (result.Contains("{id}"))
        {
            result = result.Replace("{id}", $"ID-{_rng.Next(1000, 9999)}");
        }

        if (result.Contains("{vendor}"))
        {
            result = result.Replace("{vendor}", $"VENDOR-{_rng.Next(100, 999)}");
        }

        if (result.Contains("{amount}"))
        {
            result = result.Replace("{amount}", $"{_rng.Next(100, 50000)}.{_rng.Next(10, 99)}");
        }

        if (result.Contains("{ok}"))
        {
            result = result.Replace("{ok}", _rng.Next(10, 100).ToString(CultureInfo.InvariantCulture));
        }

        if (result.Contains("{err}"))
        {
            result = result.Replace("{err}", _rng.Next(0, 5).ToString(CultureInfo.InvariantCulture));
        }

        if (result.Contains("{dept}"))
        {
            result = result.Replace("{dept}", Departments[_rng.Next(Departments.Length)]);
        }

        if (result.Contains("{email}"))
        {
            result = result.Replace("{email}", $"emp{_rng.Next(100, 999)}@empresa.com");
        }

        if (result.Contains("{date}"))
        {
            result = result.Replace("{date}", DateTime.UtcNow.AddDays(-_rng.Next(1, 30)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        if (result.Contains("{pct}"))
        {
            result = result.Replace("{pct}", _rng.Next(95, 100).ToString(CultureInfo.InvariantCulture));
        }

        if (result.Contains("{queue}"))
        {
            result = result.Replace("{queue}", "SUPPORT-QUEUE");
        }

        if (result.Contains("{title}"))
        {
            result = result.Replace("{title}", $"Incidencia #{_rng.Next(1000, 9999)}");
        }

        if (result.Contains("{category}"))
        {
            result = result.Replace("{category}", Categories[_rng.Next(Categories.Length)]);
        }

        if (result.Contains("{team}"))
        {
            result = result.Replace("{team}", Teams[_rng.Next(Teams.Length)]);
        }

        return result;
    }

    private List<ExecutionLogSnapshot> GenerateInitialLogs(JobSnapshot job)
    {
        var processIndex = GetProcessIndex(job.ProcessExternalId);
        var loggerName = LoggerNames[processIndex];
        var templates = GetLogTemplates(processIndex);
        var now = DateTime.UtcNow;
        var logs = new List<ExecutionLogSnapshot>();

        var count = Math.Min(3, templates.Length);
        for (int i = 0; i < count; i++)
        {
            logs.Add(new ExecutionLogSnapshot(
                JobExternalId: job.ExternalId,
                RobotExternalId: job.RobotExternalId,
                ProcessExternalId: job.ProcessExternalId,
                TimestampUtc: now.AddSeconds(i),
                Severity: "Info",
                LoggerName: loggerName,
                Message: FillTemplate(templates[i])));
        }

        return logs;
    }

    #endregion

    #region Timer

    private void OnTimerTick(object? state)
    {
        lock (_lock)
        {
            RotateRobotStatus();
            CompleteStaleRunningJobs();
            FluctuateQueuePendingItems();
            MaybeAddNewRunningJob();
        }
    }

    private void RotateRobotStatus()
    {
        var now = DateTime.UtcNow;
        var idleIndex = _robots.FindIndex(r => r.Status == "Idle");
        if (idleIndex >= 0)
        {
            _robots[idleIndex] = _robots[idleIndex] with { Status = "Busy", LastHeartbeatUtc = now };
        }
        else
        {
            var busyIndex = _robots.FindIndex(r => r.Status == "Busy");
            if (busyIndex >= 0)
            {
                _robots[busyIndex] = _robots[busyIndex] with { Status = "Idle", LastHeartbeatUtc = now };
            }
        }
    }

    private void CompleteStaleRunningJobs()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-2);
        for (int i = 0; i < _jobs.Count; i++)
        {
            var j = _jobs[i];
            if (j.Status == "Running" && j.StartTimeUtc < cutoff)
            {
                var success = _rng.NextDouble() < 0.9;
                var endTime = DateTime.UtcNow;
                _jobs[i] = j with
                {
                    Status = success ? "Success" : "Failed",
                    EndTimeUtc = endTime,
                    Duration = endTime - j.StartTimeUtc,
                    ErrorType = success ? null : (_rng.Next(2) == 0 ? "BusinessException" : "SystemException"),
                    ErrorMessage = success ? null : "Error durante simulación de ejecución"
                };
            }
        }
    }

    private void FluctuateQueuePendingItems()
    {
        for (int i = 0; i < _queues.Count; i++)
        {
            var q = _queues[i];
            int delta = _rng.Next(-3, 4);
            int newPending = Math.Max(0, q.PendingItems + delta);
            int newTotal = q.ProcessedItems + q.FailedItems + newPending;
            _queues[i] = q with { PendingItems = newPending, TotalItems = newTotal };
        }
    }

    private void MaybeAddNewRunningJob()
    {
        if (_rng.NextDouble() >= 0.5)
        {
            return;
        }

        var proc = _processes[_rng.Next(_processes.Count)];
        var availableRobots = _robots.Where(r => r.Status is "Idle" or "Online").ToList();
        if (availableRobots.Count == 0)
        {
            return;
        }

        var robot = availableRobots[_rng.Next(availableRobots.Count)];
        var job = new JobSnapshot(
            ExternalId: $"job-sim-{Guid.NewGuid():N}",
            ProcessExternalId: proc.ExternalId,
            RobotExternalId: robot.ExternalId,
            MachineExternalId: robot.MachineExternalId,
            Status: "Running",
            StartTimeUtc: DateTime.UtcNow,
            EndTimeUtc: null,
            Duration: null,
            ErrorType: null,
            ErrorMessage: null);

        _jobs.Add(job);
        _logs.AddRange(GenerateInitialLogs(job));
    }

    #endregion

    #region Public Read Methods

    public IReadOnlyList<RobotSnapshot> GetRobots()
    {
        lock (_lock)
        {
            return _robots.ToList();
        }
    }

    public IReadOnlyList<MachineSnapshot> GetMachines()
    {
        lock (_lock)
        {
            return _machines.ToList();
        }
    }

    public IReadOnlyList<ProcessSnapshot> GetProcesses()
    {
        lock (_lock)
        {
            return _processes.ToList();
        }
    }

    public IReadOnlyList<ProcessParameter> GetProcessParameters(string processExternalId)
    {
        lock (_lock)
        {
            return _processParams.TryGetValue(processExternalId, out var parameters)
                ? parameters.ToList()
                : new List<ProcessParameter>();
        }
    }

    public IReadOnlyList<JobSnapshot> GetJobs()
    {
        lock (_lock)
        {
            return _jobs.ToList();
        }
    }

    public IReadOnlyList<QueueSnapshot> GetQueues()
    {
        lock (_lock)
        {
            return _queues.ToList();
        }
    }

    public IReadOnlyList<QueueItemSnapshot> GetQueueItems()
    {
        lock (_lock)
        {
            return _queueItems.ToList();
        }
    }

    public IReadOnlyList<ExecutionLogSnapshot> GetLogs()
    {
        lock (_lock)
        {
            return _logs.ToList();
        }
    }

    public IReadOnlyList<AssetMetadata> GetAssets()
    {
        lock (_lock)
        {
            return _assets.ToList();
        }
    }

    #endregion

    #region Public Write Methods

    public StartJobResult StartJob(StartJobRequest request)
    {
        lock (_lock)
        {
            var process = _processes.FirstOrDefault(p => p.ExternalId == request.ProcessExternalId);
            if (process is null)
            {
                throw new InvalidOperationException(
                    $"Process '{request.ProcessExternalId}' not found in demo data.");
            }

            var robotId = request.RobotExternalId;
            if (string.IsNullOrEmpty(robotId))
            {
                var available = _robots.FirstOrDefault(r => r.Status is "Idle" or "Online");
                robotId = available?.ExternalId ?? "robot-01";
            }

            var machineId = GetMachineForRobot(robotId);
            var externalId = Guid.NewGuid().ToString();

            var job = new JobSnapshot(
                ExternalId: externalId,
                ProcessExternalId: request.ProcessExternalId,
                RobotExternalId: robotId,
                MachineExternalId: machineId,
                Status: "Running",
                StartTimeUtc: DateTime.UtcNow,
                EndTimeUtc: null,
                Duration: null,
                ErrorType: null,
                ErrorMessage: null);

            _jobs.Add(job);
            _logs.AddRange(GenerateInitialLogs(job));

            return new StartJobResult(externalId);
        }
    }

    public void TransitionJob(string externalId, string newStatus)
    {
        lock (_lock)
        {
            var index = _jobs.FindIndex(j => j.ExternalId == externalId);
            if (index < 0)
            {
                return;
            }

            var job = _jobs[index];
            if (job.Status != "Running")
            {
                return;
            }

            var endTime = DateTime.UtcNow;
            _jobs[index] = job with
            {
                Status = newStatus,
                EndTimeUtc = endTime,
                Duration = endTime - job.StartTimeUtc
            };
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _timer.Dispose();
    }

    #endregion
}

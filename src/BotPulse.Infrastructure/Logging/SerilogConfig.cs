using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace BotPulse.Infrastructure.Logging;

/// <summary>Configures Serilog structured logging for BotPulse.</summary>
public static class SerilogConfig
{
    /// <summary>
    /// Applies BotPulse Serilog configuration to the host builder.
    /// Reads from IConfiguration (appsettings.json + env vars) and adds Console and File sinks.
    /// </summary>
    public static IHostBuilder UseBotPulseSerilog(this IHostBuilder builder) =>
        builder.UseSerilog((ctx, cfg) =>
        {
            cfg.ReadFrom.Configuration(ctx.Configuration)
               .Enrich.FromLogContext()
               .Enrich.WithMachineName()
               .Enrich.WithEnvironmentName()
               .Enrich.WithThreadId()
               .Enrich.WithProperty("Application", "BotPulse")
               .WriteTo.Console(outputTemplate:
                   "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {SourceContext}: {Message:lj}{NewLine}{Exception}",
                   formatProvider: System.Globalization.CultureInfo.InvariantCulture,
                   restrictedToMinimumLevel: LogEventLevel.Debug)
               .WriteTo.File(
                   path: "logs/botpulse-.log",
                   rollingInterval: RollingInterval.Day,
                   retainedFileCountLimit: 30,
                   formatProvider: System.Globalization.CultureInfo.InvariantCulture,
                   restrictedToMinimumLevel: LogEventLevel.Information);
        });
}

using BotPulse.Core.Application.Assets;
using BotPulse.Core.Application.Auth;
using BotPulse.Core.Application.Dashboard;
using BotPulse.Core.Application.Jobs;
using BotPulse.Core.Application.Logs;
using BotPulse.Core.Application.Machines;
using BotPulse.Core.Application.Metrics;
using BotPulse.Core.Application.Processes;
using BotPulse.Core.Application.Queues;
using BotPulse.Core.Application.Robots;
using BotPulse.Core.Application.Alerts;
using BotPulse.Infrastructure.Authentication;
using BotPulse.Infrastructure.DependencyInjection;
using BotPulse.Infrastructure.Logging;
using BotPulse.Providers.UiPath.DependencyInjection;
using BotPulse.Providers.Demo.DependencyInjection;
using BotPulse.Api.Middleware;
using BotPulse.Api.Authorization;
using BotPulse.Authorization.Permissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BotPulse.Core.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseBotPulseSerilog();

// Persistence + Infrastructure
builder.Services.AddBotPulsePersistence(builder.Configuration);
builder.Services.AddBotPulseInfrastructure(builder.Configuration);

// RPA Provider — selection by configuration
var rpaProvider = builder.Configuration["RpaProvider"] ?? "Demo";
if (rpaProvider.Equals("UiPath", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddUiPathProvider(builder.Configuration);
}
else
{
    builder.Services.AddDemoProvider();
}

// Application Services
builder.Services.AddScoped<RobotQueryService>();
builder.Services.AddScoped<MachineQueryService>();
builder.Services.AddScoped<ProcessQueryService>();
builder.Services.AddScoped<AssetQueryService>();
builder.Services.AddScoped<JobQueryService>();
builder.Services.AddScoped<JobCommandService>();
builder.Services.AddScoped<QueueQueryService>();
builder.Services.AddScoped<QueueAnalyticsService>();
builder.Services.AddScoped<LogQueryService>();
builder.Services.AddScoped<MetricsQueryService>();
builder.Services.AddScoped<MetricsAggregationService>();
builder.Services.AddScoped<DashboardConfigurationService>();
builder.Services.AddScoped<AuthenticationOrchestrator>();
builder.Services.AddScoped<AlertRuleService>();
builder.Services.AddScoped<AlertAcknowledgmentService>();

// Cache options for read-on-demand services
builder.Services.AddSingleton(new RobotCacheOptions { Enabled = true, TtlSeconds = 120 });
builder.Services.AddSingleton(new MachineCacheOptions { Enabled = true, TtlSeconds = 300 });
builder.Services.AddSingleton(new ProcessCacheOptions { Enabled = true, TtlSeconds = 600 });
builder.Services.AddSingleton(new QueueCacheOptions { Enabled = true, TtlSeconds = 180 });

// System Clock
builder.Services.AddSingleton<BotPulse.Core.Abstractions.Time.ISystemClock,
    BotPulse.Infrastructure.Time.SystemClock>();

// API Versioning
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = false;
    opt.ReportApiVersions = true;
    opt.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("api-version"));
});

builder.Services.AddVersionedApiExplorer(opt =>
{
    opt.GroupNameFormat = "'v'VVV";
    opt.SubstituteApiVersionInUrl = true;
});

// Controllers + FluentValidation auto-validation
builder.Services.AddControllers();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// JWT Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection["SigningKeyBase64"] ?? string.Empty;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Convert.FromBase64String(signingKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(10),
        };
        // Allow token via query param for SSE (EventSource doesn't support Authorization headers)
        opt.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["token"].FirstOrDefault();
                if (!string.IsNullOrEmpty(token) &&
                    ctx.Request.Path.StartsWithSegments("/api/v1/notifications/stream"))
                {
                    ctx.Token = token;
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = async ctx =>
            {
                var services = ctx.HttpContext.RequestServices;
                var userRepo = services.GetService<IUserRepository>();
                var authService = services.GetService<BotPulse.Authorization.IAuthorizationService>();

                if (userRepo == null || authService == null)
                {
                    return;
                }

                var principal = ctx.Principal;
                var externalId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var provider = principal?.FindFirst("auth_provider")?.Value;

                if (string.IsNullOrEmpty(externalId) || string.IsNullOrEmpty(provider))
                {
                    return;
                }

                var user = await userRepo.FindByExternalIdAsync(provider, externalId);
                if (user == null)
                {
                    return;
                }

                var permissions = await authService.GetPermissionsAsync(user.Id);
                if (permissions == null)
                {
                    return;
                }

                if (principal?.Identity is ClaimsIdentity id)
                {
                    // Add internal user ID claim for use in controllers
                    id.AddClaim(new Claim("internal_user_id", user.Id.ToString()));

                    foreach (var p in permissions)
                    {
                        id.AddClaim(new Claim("permission", p));
                    }
                }
            }
        };
    });

builder.Services.AddAuthorization(opt =>
{
    // Legacy role-based policies (kept for backward compatibility during migration)
    opt.AddPolicy("RequireOperator",      p => p.RequireRole("Operator", "Administrator"));
    opt.AddPolicy("RequireAdministrator", p => p.RequireRole("Administrator"));
    opt.AddPolicy("ViewAssets",           p => p.RequireRole("Administrator"));
    opt.AddPolicy("ManageAlertRules",     p => p.RequireRole("Administrator"));
    opt.AddPolicy("JobActions",           p => p.RequireRole("Operator", "Administrator"));

    // Permission-based policies — one per PermissionCatalog constant
    foreach (var permission in PermissionCatalog.All)
    {
        opt.AddPolicy(permission, policy =>
            policy.Requirements.Add(new PermissionRequirement(permission)));
    }
});

// Register permission authorization handler
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// IHttpContextAccessor required by HttpContextAuthorizationContextAccessor
builder.Services.AddHttpContextAccessor();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new() { Title = "BotPulse API", Version = "v1" });
    opt.AddSecurityDefinition("Bearer", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT token",
    });
    opt.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// Health Checks
var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("PostgreSQL") ?? string.Empty,
        name: "database",
        tags: ["ready"]);

if (rpaProvider.Equals("UiPath", StringComparison.OrdinalIgnoreCase))
{
    healthChecksBuilder.AddUiPathHealthCheck("rpa-provider", "ready");
}
else
{
    healthChecksBuilder.AddDemoHealthCheck("rpa-provider", "ready");
}

// CORS
var corsOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ?? [];
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Apply pending EF Core migrations on startup (safe for single-instance deployment)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BotPulse.Infrastructure.Persistence.BotPulseDbContext>();
    await db.Database.MigrateAsync();
}

// Middleware pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ErrorHandlerMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/swagger/v1/swagger.json", "BotPulse API v1");
    opt.RoutePrefix = "swagger";
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuthorizationContextMiddleware>();
app.UseMiddleware<AuditMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false,
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = h => h.Tags.Contains("ready"),
});

app.Run();

// Make Program accessible for integration tests
public partial class Program { }

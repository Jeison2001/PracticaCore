using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Api.Middlewares;
using Domain.Common.Dashboard;
using Domain.Common.Notifications;
using Domain.Entities;
using Domain.Interfaces.Services.Notifications;
using Infrastructure.Data;
using Infrastructure.Services.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Api.Extensions
{
    public static class HangfireExtensions
    {
        /// <summary>
        /// Registra Hangfire con PostgreSQL.
        /// </summary>
        public static IServiceCollection AddHangfireDefault(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Hangfire");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("La cadena de conexión 'Hangfire' no está configurada.");

            var schemaName = configuration["Hangfire:SchemaName"];
            if (string.IsNullOrWhiteSpace(schemaName))
                schemaName = "hangfire";
            else
                schemaName = schemaName.Trim();

            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
                {
                    SchemaName = schemaName,
                    EnableTransactionScopeEnlistment = true
                }));

            return services;
        }

        /// <summary>
        /// Registra el Dashboard de Hangfire como endpoint (compatible con AllowAnonymous).
        /// - Localhost: acceso libre (sin pop-up de contraseña).
        /// - Producción: Basic Auth con usuario/password de appsettings.
        /// </summary>
        public static IEndpointConventionBuilder MapHangfireConfiguration(
            this IEndpointRouteBuilder endpoints,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            var user = configuration["Hangfire:Dashboard:User"]
                ?? throw new InvalidOperationException("Hangfire:Dashboard:User no está configurado en appsettings.");
            var pass = configuration["Hangfire:Dashboard:Password"]
                ?? throw new InvalidOperationException("Hangfire:Dashboard:Password no está configurado en appsettings.");

            return endpoints.MapHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[]
                {
                    new HangfireAuthorizationFilter(configuration, env)
                },
                DashboardTitle = "PracticaCore - Hangfire Dashboard",
                IgnoreAntiforgeryToken = true
            }).AllowAnonymous();
        }

        /// <summary>
        /// Registra trabajos recurrentes de Hangfire (alertas SLA diarias en zona horaria Colombia).
        /// </summary>
        public static void RegisterHangfireRecurringJobs(
            this IApplicationBuilder app,
            IConfiguration configuration)
        {
            ILogger? logger = null;
            try
            {
                logger = app.ApplicationServices.GetService<ILoggerFactory>()?.CreateLogger(nameof(HangfireExtensions));

                using var scope = app.ApplicationServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetService<AppDbContext>();
                SlaAlertConfiguration? dbConfig = null;

                if (dbContext != null)
                {
                    try
                    {
                        dbConfig = dbContext.SlaAlertConfigurations
                            .AsNoTracking()
                            .FirstOrDefault(x => x.Id == 1 && x.StatusRegister);
                    }
                    catch (Exception dbEx)
                    {
                        logger?.LogWarning(
                            dbEx,
                            "No fue posible consultar SlaAlertConfiguration al iniciar; se conservan los jobs recurrentes persistidos.");
                        return;
                    }
                }

                if (dbConfig != null)
                {
                    var scheduler = scope.ServiceProvider.GetService<ISlaRecurringJobScheduler>();
                    if (scheduler != null)
                    {
                        scheduler.SyncRecurringJob(
                            dbConfig.IsEnabled,
                            dbConfig.ScheduleHourColombia,
                            dbConfig.ScheduleMinuteColombia,
                            dbConfig.WeekdaysOnly);
                        return;
                    }

                    if (!dbConfig.IsEnabled)
                    {
                        RecurringJob.RemoveIfExists(SlaRecurringJobScheduler.SlaDeadlineAlertsJobId);
                        return;
                    }

                    var dbCron = SlaCronHelper.BuildCron(
                        dbConfig.ScheduleHourColombia,
                        dbConfig.ScheduleMinuteColombia,
                        dbConfig.WeekdaysOnly);

                    RecurringJob.AddOrUpdate<ISlaDeadlineAlertService>(
                        SlaRecurringJobScheduler.SlaDeadlineAlertsJobId,
                        service => service.ProcessDueAlertsAsync(CancellationToken.None),
                        dbCron,
                        new RecurringJobOptions
                        {
                            TimeZone = DashboardTimeHelper.ColombiaTimeZone
                        });
                    return;
                }

                var enabled = configuration.GetValue("SlaAlerts:Enabled", true);
                if (!enabled)
                {
                    RecurringJob.RemoveIfExists(SlaRecurringJobScheduler.SlaDeadlineAlertsJobId);
                    return;
                }

                var rawCron = configuration["SlaAlerts:Cron"];
                var cron = string.IsNullOrWhiteSpace(rawCron) ? "30 7 * * *" : rawCron.Trim();

                RecurringJob.AddOrUpdate<ISlaDeadlineAlertService>(
                    SlaRecurringJobScheduler.SlaDeadlineAlertsJobId,
                    service => service.ProcessDueAlertsAsync(CancellationToken.None),
                    cron,
                    new RecurringJobOptions
                    {
                        TimeZone = DashboardTimeHelper.ColombiaTimeZone
                    });
            }
            catch (Exception ex)
            {
                logger ??= app.ApplicationServices.GetService<ILoggerFactory>()?.CreateLogger(nameof(HangfireExtensions));
                logger?.LogWarning(
                    ex,
                    "Error al registrar/sincronizar trabajos recurrentes de Hangfire; la aplicación continuará iniciando.");
            }
        }
    }
}

using Hangfire;
using Hangfire.PostgreSql;

namespace Api.Extensions
{
    public static class HangfireExtensions
    {
        public static IServiceCollection AddHangfireConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Default");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("La cadena de conexión 'Default' no está configurada.");
            }

            // Configurar Hangfire con PostgreSQL - Configuración optimizada para rendimiento
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
                {
                    // Configuraciones MUY conservadoras para Supabase
                    QueuePollInterval = TimeSpan.FromSeconds(60), // Mucho menos frecuente
                    JobExpirationCheckInterval = TimeSpan.FromHours(6), // Muy poco frecuente
                    CountersAggregateInterval = TimeSpan.FromMinutes(30), // Muy poco frecuente
                    PrepareSchemaIfNecessary = true,
                    
                    // Timeouts MUY generosos para conexiones inestables
                    TransactionSynchronisationTimeout = TimeSpan.FromMinutes(2), // 2 minutos
                    InvisibilityTimeout = TimeSpan.FromHours(1), // 1 hora
                    DistributedLockTimeout = TimeSpan.FromMinutes(10), // 10 minutos
                    
                    // Configuración específica para PostgreSQL
                    SchemaName = "hangfire",
                    EnableTransactionScopeEnlistment = true
                }));

            // Configurar servidor con valores MUY conservadores para Supabase
            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 1; // Solo 1 worker para minimizar conexiones
                options.Queues = new[] { "default" }; // Solo 1 cola esencial
                options.HeartbeatInterval = TimeSpan.FromMinutes(2); // Cada 2 minutos
                options.ServerTimeout = TimeSpan.FromMinutes(10); // Timeout muy generoso
                options.SchedulePollingInterval = TimeSpan.FromMinutes(1); // Cada minuto
                options.ServerCheckInterval = TimeSpan.FromMinutes(5); // Cada 5 minutos
                options.CancellationCheckInterval = TimeSpan.FromMinutes(1); // Cada minuto
            });
            // Configuración global de reintentos para conexiones inestables
            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
            {
                Attempts = 5, // Más reintentos por conexiones inestables
                DelaysInSeconds = new[] { 60, 300, 900, 1800, 3600 }, // 1min, 5min, 15min, 30min, 1h
                LogEvents = false // Desactivar logging excesivo
            });

            return services;
        }

        public static IApplicationBuilder UseHangfireConfiguration(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            // OPCIÓN 1: Dashboard solo en Development (más seguro para producción)
            // if (env.IsDevelopment())
            // {
            //     app.UseHangfireDashboard("/hangfire", new DashboardOptions
            //     {
            //         DashboardTitle = "PracticaCore - Hangfire Dashboard (Development)"
            //     });
            // }

            // OPCIÓN 2: Dashboard en todos los entornos (descomenta si quieres usarlo)
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new Api.Middlewares.HangfireAuthorizationFilter() },
                DashboardTitle = "PracticaCore - Hangfire Dashboard"
            });

            return app;
        }

        public static void ConfigureHangfireJobs(this IApplicationBuilder app)
        {
            // Job de limpieza semanal (menos frecuente para evitar sobrecarga)
            RecurringJob.AddOrUpdate("hangfire-cleanup", 
                () => CleanupOldJobs(), 
                Cron.Weekly(DayOfWeek.Sunday, 3)); // Cada domingo a las 3 AM
        }

        // Método de limpieza optimizado para conexiones inestables
        public static void CleanupOldJobs()
        {
            try
            {
                var monitoring = JobStorage.Current.GetMonitoringApi();
                
                // Procesar en lotes pequeños para evitar timeouts
                const int batchSize = 100;
                
                // Eliminar jobs completados más antiguos de 30 días (en lotes)
                var succeededJobs = monitoring.SucceededJobs(0, batchSize);
                var oldSucceededJobs = succeededJobs.Where(j => j.Value.SucceededAt < DateTime.UtcNow.AddDays(-30));
                foreach (var job in oldSucceededJobs)
                {
                    BackgroundJob.Delete(job.Key);
                    Thread.Sleep(100); // Pausa entre eliminaciones
                }
                
                // Eliminar jobs fallidos más antiguos de 30 días (en lotes)
                var failedJobs = monitoring.FailedJobs(0, batchSize);
                var oldFailedJobs = failedJobs.Where(j => j.Value.FailedAt < DateTime.UtcNow.AddDays(-30));
                foreach (var job in oldFailedJobs)
                {
                    BackgroundJob.Delete(job.Key);
                    Thread.Sleep(100); // Pausa entre eliminaciones
                }
            }
            catch (Exception)
            {
                // Si falla la limpieza, no es crítico - se intentará la próxima semana
            }
        }
    }
}

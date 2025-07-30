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

            // Configurar Hangfire con PostgreSQL - Configuración equilibrada
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
                {
                    // Configuraciones básicas pero efectivas
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true,
                    
                    // Timeouts razonables para evitar problemas de conexión
                    TransactionSynchronisationTimeout = TimeSpan.FromSeconds(30),
                    InvisibilityTimeout = TimeSpan.FromMinutes(30),
                    DistributedLockTimeout = TimeSpan.FromMinutes(5),
                    
                    // Configuración específica para PostgreSQL
                    SchemaName = "hangfire",
                    EnableTransactionScopeEnlistment = true
                }));

            // Configurar servidor con valores predeterminados sensatos
            services.AddHangfireServer(options =>
            {
                options.WorkerCount = Math.Max(Environment.ProcessorCount, 1);
                options.Queues = new[] { "default", "critical", "normal" };
                options.HeartbeatInterval = TimeSpan.FromSeconds(30);
                options.ServerTimeout = TimeSpan.FromMinutes(4);
                options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
            });
            // Configuración global de reintentos por defecto
            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
            {
                Attempts = 3,
                DelaysInSeconds = new[] { 10, 30, 60 } // 10s, 30s, 1min
            });

            return services;
        }

        public static IApplicationBuilder UseHangfireConfiguration(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            // OPCIÓN 1: Dashboard solo en Development (más seguro para producción)
            // if (env.IsDevelopment())
            // {
                app.UseHangfireDashboard("/hangfire", new DashboardOptions
                {
                    DashboardTitle = "PracticaCore - Hangfire Dashboard (Development)"
                });
            // }


            return app;
        }

        public static void ConfigureHangfireJobs(this IApplicationBuilder app)
        {
            // Configurar jobs recurrentes si es necesario
            // Ejemplo: RecurringJob.AddOrUpdate("test-job", () => Console.WriteLine("Job ejecutado"), Cron.Daily);
        }
    }
}

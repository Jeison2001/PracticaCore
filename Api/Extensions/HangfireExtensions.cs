using Hangfire;
using Hangfire.PostgreSql;

namespace Api.Extensions
{
    public static class HangfireExtensions
    {
        /// <summary>
        /// Configuración recomendada de Hangfire
        /// </summary>
        public static IServiceCollection AddHangfireDefault(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Hangfire");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("La cadena de conexión 'Hangfire' no está configurada.");

            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
                {
                    SchemaName = "hangfire",
                    EnableTransactionScopeEnlistment = true
                }));

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
    }
}

using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Api.Middlewares;

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
    }
}

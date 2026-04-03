using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Domain.Interfaces.Services.Jobs;
using Tests.Integration.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public List<EnqueuedJobInfo> EnqueuedJobs { get; } = new();
        private string? _initialDbName;
        private readonly object _lock = new();

        public CustomWebApplicationFactory()
        {
            // Establecer variable de entorno para indicar a Program.cs que omita el registro de Npgsql
            Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Usar configuración específica de tests para evitar exponer credenciales reales
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Jwt:Key", "test-key-for-integration-tests-minimum-32-characters-required"},
                    {"Jwt:Issuer", "test-issuer"},
                    {"Jwt:Audience", "test-audience"},
                    {"UseInMemoryDatabase", "true"}
                });
            });

            builder.ConfigureServices(services =>
            {
                // Agregar AppDbContext usando una base de datos en memoria para tests
                // Usar un nombre único para cada instancia de factory para aislar tests
                _initialDbName = Guid.NewGuid().ToString();
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_initialDbName);
                    options.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                    // Deshabilitar generación de proxy (lazy loading requiere el paquete proxy que no está instalado)
                    options.ConfigureWarnings(w => w.Throw(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });

                // Registrar la lista de jobs encolados como singleton para que TestJobEnqueuer pueda accederla
                services.AddSingleton(EnqueuedJobs);

                // Reemplazar IJobEnqueuer con TestJobEnqueuer
                // Esto evita que la aplicación intente usar Hangfire (que no está configurado en tests)
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IJobEnqueuer));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddTransient<IJobEnqueuer, TestJobEnqueuer>();

                // Deshabilitar FallbackPolicy para tests de integración - los tests usan su propia autenticación cuando la necesitan
                services.Configure<AuthorizationOptions>(options =>
                {
                    options.FallbackPolicy = null;
                });

                // Agregar handler de autenticación de test que auto-autentica todas las solicitudes
                services.AddAuthentication("TestScheme")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", null);

                // Establecer TestScheme como esquema por defecto para tests de integración
                services.Configure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
                {
                    options.DefaultScheme = "TestScheme";
                    options.DefaultAuthenticateScheme = "TestScheme";
                });
            });

            // Usar entorno "Testing" para evitar inicialización de Hangfire y obtener comportamiento específico de test
            builder.UseEnvironment("Testing");
        }

        /// <summary>
        /// Crea una NUEVA base de datos en memoria y retorna un AppDbContext fresco.
        /// Usar esto para asegurar el aislamiento de tests entre métodos de test.
        /// </summary>
        public AppDbContext CreateNewDbContext()
        {
            lock (_lock)
            {
                var serviceProvider = Services.GetService<IServiceProvider>();
                if (serviceProvider == null)
                    throw new InvalidOperationException("ServiceProvider not available");

                // Crear una nueva base de datos en memoria con nombre único
                var newDbName = Guid.NewGuid().ToString();

                // Crear un nuevo scope de servicios
                var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Usar la nueva base de datos
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                return context;
            }
        }

        protected override void Dispose(bool disposing)
        {
            // Limpiar la base de datos en memoria después de los tests
            base.Dispose(disposing);
        }
    }
}

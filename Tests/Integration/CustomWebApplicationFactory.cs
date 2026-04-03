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
            // Set environment variable to signal Program.cs to skip Npgsql registration
            Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Use test-specific configuration to avoid exposing real credentials
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
                // Add AppDbContext using an in-memory database for testing
                // Use a unique name for each factory instance to isolate tests
                _initialDbName = Guid.NewGuid().ToString();
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_initialDbName);
                    options.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                    // Disable proxy generation (lazy loading requires proxy package which is not installed)
                    options.ConfigureWarnings(w => w.Throw(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });

                // Register the enqueued jobs list as a singleton so it can be accessed by TestJobEnqueuer
                services.AddSingleton(EnqueuedJobs);

                // Replace IJobEnqueuer with TestJobEnqueuer
                // This prevents the application from trying to use Hangfire (which is not configured in tests)
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IJobEnqueuer));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddTransient<IJobEnqueuer, TestJobEnqueuer>();

                // Disable FallbackPolicy for integration tests - tests use their own auth when needed
                services.Configure<AuthorizationOptions>(options =>
                {
                    options.FallbackPolicy = null;
                });

                // Add test authentication handler that auto-authenticates all requests
                services.AddAuthentication("TestScheme")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", null);

                // Set TestScheme as default for integration tests
                services.Configure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
                {
                    options.DefaultScheme = "TestScheme";
                    options.DefaultAuthenticateScheme = "TestScheme";
                });
            });

            // Use "Testing" environment to avoid Hangfire initialization and get specific test behavior
            builder.UseEnvironment("Testing");
        }

        /// <summary>
        /// Creates a NEW in-memory database and returns a fresh AppDbContext.
        /// Use this to ensure test isolation between test methods.
        /// </summary>
        public AppDbContext CreateNewDbContext()
        {
            lock (_lock)
            {
                var serviceProvider = Services.GetService<IServiceProvider>();
                if (serviceProvider == null)
                    throw new InvalidOperationException("ServiceProvider not available");

                // Create a new in-memory database with unique name
                var newDbName = Guid.NewGuid().ToString();

                // Create a new service scope
                var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Use the new database
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                return context;
            }
        }

        protected override void Dispose(bool disposing)
        {
            // Clean up in-memory database after tests
            base.Dispose(disposing);
        }
    }
}

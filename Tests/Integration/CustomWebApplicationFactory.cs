using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Data;
using Microsoft.Extensions.Configuration;

namespace Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
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
                var dbName = Guid.NewGuid().ToString();
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });
            });

            // Use "Testing" environment to avoid running production configurations
            builder.UseEnvironment("Testing");
        }

        protected override void Dispose(bool disposing)
        {
            // Clean up in-memory database after tests
            base.Dispose(disposing);
        }
    }
}

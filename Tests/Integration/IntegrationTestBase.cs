using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Data;
using Xunit;
using Tests.Integration.Utilities;

namespace Tests.Integration
{
    public class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IDisposable
    {
        protected readonly CustomWebApplicationFactory _factory;
        protected readonly HttpClient _client;
        protected readonly IServiceScope _scope;
        protected readonly AppDbContext _context;
        private readonly AppDbContext _originalContext;

        public IntegrationTestBase(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();

            _scope = factory.Services.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _originalContext = _context;

            // Clear any previously enqueued jobs before each test
            _factory.EnqueuedJobs.Clear();

            // Create a FRESH in-memory database for THIS test to ensure isolation
            // This prevents theory rows from interfering with each other
            ResetForIsolation();
        }

        /// <summary>
        /// Gets a fresh AppDbContext with a clean in-memory database.
        /// Use this instead of _context or _scope.ServiceProvider.GetRequiredService<AppDbContext>()
        /// to ensure test isolation between theory rows.
        /// </summary>
        protected AppDbContext GetFreshDbContext()
        {
            // Clear the old context's tracker
            _originalContext.ChangeTracker.Clear();

            // Return a new context with a fresh in-memory database
            return _factory.CreateNewDbContext();
        }

        private void ResetForIsolation()
        {
            // Ensure we have a clean slate for this test
            _originalContext.ChangeTracker.Clear();
        }

        public void Dispose()
        {
            _scope.Dispose();
            _client.Dispose();
        }
    }
}

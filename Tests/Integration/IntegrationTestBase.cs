using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Data;
using Xunit;

namespace Tests.Integration
{
    public class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IDisposable
    {
        protected readonly CustomWebApplicationFactory _factory;
        protected readonly HttpClient _client;
        protected readonly AppDbContext _context;
        protected readonly IServiceScope _scope;

        public IntegrationTestBase(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            
            _scope = factory.Services.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        }

        public void Dispose()
        {
            _scope.Dispose();
            _client.Dispose();
        }
    }
}

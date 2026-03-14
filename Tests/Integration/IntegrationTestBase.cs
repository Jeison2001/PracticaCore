using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Data;
using Xunit;

namespace Tests.Integration
{
    public class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
    {
        protected readonly CustomWebApplicationFactory _factory;
        protected readonly HttpClient _client;
        protected readonly AppDbContext _context;

        public IntegrationTestBase(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            
            var scope = factory.Services.CreateScope();
            _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        }
    }
}

using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Tests.UnitTests.Infrastructure
{
    public class BaseRepositoryTests
    {
        private readonly Mock<AppDbContext> _mockContext;

        public BaseRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;
            _mockContext = new Mock<AppDbContext>(options);
        }
    }
}

using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Tests.UnitTests.Infrastructure
{
    public class BaseRepositoryTests
    {
        private readonly Mock<AppDbContext> _mockContext;
        private readonly BaseRepository<Example, int> _repository;

        public BaseRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;
            _mockContext = new Mock<AppDbContext>(options);
            _repository = new BaseRepository<Example, int>(_mockContext.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsEntity_WhenEntityExists()
        {
            // Arrange
            var example = new Example { Id = 1, Name = "Test" };
            _mockContext.Setup(m => m.Set<Example>().FindAsync(1))
                .ReturnsAsync(example);

            // Act
            var result = await _repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            // Arrange
            var examples = new List<Example>
                {
                    new() { Id = 1, Name = "Test1" },
                    new() { Id = 2, Name = "Test2" }
                };

            var mockDbSet = new Mock<DbSet<Example>>();
            var queryableExamples = examples.AsQueryable();

            mockDbSet.As<IQueryable<Example>>().Setup(m => m.Provider).Returns(queryableExamples.Provider);
            mockDbSet.As<IQueryable<Example>>().Setup(m => m.Expression).Returns(queryableExamples.Expression);
            mockDbSet.As<IQueryable<Example>>().Setup(m => m.ElementType).Returns(queryableExamples.ElementType);
            mockDbSet.As<IQueryable<Example>>().Setup(m => m.GetEnumerator()).Returns(() => queryableExamples.GetEnumerator());
            mockDbSet.As<IAsyncEnumerable<Example>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>())).Returns(new TestAsyncEnumerator<Example>(queryableExamples.GetEnumerator()));

            _mockContext.Setup(m => m.Set<Example>()).Returns(mockDbSet.Object);

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task AddAsync_AddsEntity()
        {
            // Arrange
            var example = new Example { Id = 1, Name = "Test" };
            var mockDbSet = new Mock<DbSet<Example>>();
            _mockContext.Setup(m => m.Set<Example>()).Returns(mockDbSet.Object);

            // Act
            await _repository.AddAsync(example);

            // Assert
            _mockContext.Verify(m => m.Set<Example>(), Times.Once);
            mockDbSet.Verify(m => m.AddAsync(example, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesEntity()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "UpdateTest")
                .Options;

            // Create a real AppDbContext with the in-memory provider
            using var context = new AppDbContext(options);
            var repository = new BaseRepository<Example, int>(context);

            var example = new Example { Id = 1, Name = "Test" };
            await context.Set<Example>().AddAsync(example);
            await context.SaveChangesAsync();

            // Update the entity
            example.Name = "Updated Name";

            // Act
            await repository.UpdateAsync(example);

            // Assert
            var updatedExample = await context.Set<Example>().FindAsync(1);
            Assert.NotNull(updatedExample);
            Assert.Equal("Updated Name", updatedExample.Name);
        }


        [Fact]
        public async Task DeleteAsync_DeletesEntity()
        {
            // Arrange
            var example = new Example { Id = 1, Name = "Test" };
            var mockDbSet = new Mock<DbSet<Example>>();
            _mockContext.Setup(m => m.Set<Example>()).Returns(mockDbSet.Object);

            // Act
            await _repository.DeleteAsync(example);

            // Assert
            _mockContext.Verify(m => m.Set<Example>(), Times.Once);
            mockDbSet.Verify(m => m.Remove(example), Times.Once);
        }
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }

        public T Current => _inner.Current;
    }
}

using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.UnitTests.Infrastructure.Repositories
{
    public class BaseRepositoryTests
    {
        private static AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetAllWithPaginationAsync_PageSizeZero_ReturnsAllItemsWithAccurateMetadata()
        {
            using var context = CreateInMemoryContext();
            var now = DateTimeOffset.UtcNow;
            context.Set<Permission>().AddRange(
                new Permission { Id = 1, Code = "P1", Description = "D1", StatusRegister = true, CreatedAt = now.AddMinutes(-3) },
                new Permission { Id = 2, Code = "P2", Description = "D2", StatusRegister = true, CreatedAt = now.AddMinutes(-2) },
                new Permission { Id = 3, Code = "P3", Description = "D3", StatusRegister = true, CreatedAt = now.AddMinutes(-1) }
            );
            await context.SaveChangesAsync();

            var repo = new BaseRepository<Permission, int>(context);

            var result = await repo.GetAllWithPaginationAsync(pageNumber: 3, pageSize: 0);

            result.Items.Should().HaveCount(3);
            result.TotalRecords.Should().Be(3);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(3);
            result.HasMoreRows.Should().BeNull();
        }

        [Fact]
        public async Task GetAllWithPaginationAsync_PageSizeZeroEmptyResult_SetsTotalZeroAndPageSizeOne()
        {
            using var context = CreateInMemoryContext();
            var repo = new BaseRepository<Permission, int>(context);

            var result = await repo.GetAllWithPaginationAsync(pageNumber: 1, pageSize: 0);

            result.Items.Should().BeEmpty();
            result.TotalRecords.Should().Be(0);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(1);
        }

        [Fact]
        public async Task GetAllWithPaginationAsync_PageSizeZeroWithSkipTotalCount_ReturnsAllItemsAndTotalMinusOne()
        {
            using var context = CreateInMemoryContext();
            context.Set<Permission>().AddRange(
                new Permission { Id = 1, Code = "P1", Description = "D1", StatusRegister = true },
                new Permission { Id = 2, Code = "P2", Description = "D2", StatusRegister = true }
            );
            await context.SaveChangesAsync();

            var repo = new BaseRepository<Permission, int>(context);

            var result = await repo.GetAllWithPaginationAsync(pageNumber: 1, pageSize: 0, skipTotalCount: true);

            result.Items.Should().HaveCount(2);
            result.TotalRecords.Should().Be(-1);
            result.PageSize.Should().Be(2);
            result.HasMoreRows.Should().BeNull();
        }
    }
}

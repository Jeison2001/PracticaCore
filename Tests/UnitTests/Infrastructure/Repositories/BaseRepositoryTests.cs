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

        [Fact]
        public async Task UpdatePartialAsync_TrackedEntity_MarksOnlySpecifiedPropertiesAsModified()
        {
            using var context = CreateInMemoryContext();
            var originalCreatedAt = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
            context.Set<Permission>().Add(new Permission
            {
                Id = 10,
                Code = "ORIG_CODE",
                Description = "Original",
                CreatedAt = originalCreatedAt,
                StatusRegister = true
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var repo = new BaseRepository<Permission, int>(context);
            var tracked = await repo.GetByIdAsync(10);
            tracked.Should().NotBeNull();

            tracked!.Description = "Updated";
            await repo.UpdatePartialAsync(tracked, [x => x.Description]);

            var entry = context.Entry(tracked);
            entry.State.Should().Be(EntityState.Modified);
            entry.Property(x => x.Description).IsModified.Should().BeTrue();
            entry.Property(x => x.Code).IsModified.Should().BeFalse();
            entry.Property(x => x.CreatedAt).IsModified.Should().BeFalse();
            entry.Property(x => x.StatusRegister).IsModified.Should().BeFalse();
            entry.Property(x => x.OperationRegister).IsModified.Should().BeFalse();
        }

        [Fact]
        public async Task UpdatePartialAsync_DetachedEntity_MarksOnlySpecifiedPropertiesAsModified()
        {
            using var context = CreateInMemoryContext();
            var originalCreatedAt = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
            context.Set<Permission>().Add(new Permission
            {
                Id = 11,
                Code = "ORIG_CODE_11",
                Description = "Original 11",
                CreatedAt = originalCreatedAt,
                StatusRegister = true
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var repo = new BaseRepository<Permission, int>(context);
            var detached = new Permission { Id = 11, Description = "Patched 11" };

            await repo.UpdatePartialAsync(detached, [x => x.Description]);

            var entry = context.Entry(detached);
            entry.State.Should().Be(EntityState.Modified);
            entry.Property(x => x.Description).IsModified.Should().BeTrue();
            entry.Property(x => x.Code).IsModified.Should().BeFalse();
            entry.Property(x => x.CreatedAt).IsModified.Should().BeFalse();
            entry.Property(x => x.StatusRegister).IsModified.Should().BeFalse();

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var persisted = await context.Set<Permission>().FindAsync(11);
            persisted.Should().NotBeNull();
            persisted!.Description.Should().Be("Patched 11");
            persisted.Code.Should().Be("ORIG_CODE_11");
        }
    }
}

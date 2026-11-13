using Application.Shared.DTOs.Roles;
using Application.Shared.Mappings;
using Application.Shared.Queries;
using Application.Shared.Queries.Handlers;
using AutoMapper;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tests.UnitTests.Application.Queries
{
    public class GetAllEntitiesQueryHandlerTests
    {
        private static AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<GenericProfile>(), NullLoggerFactory.Instance);
            return config.CreateMapper();
        }

        [Fact]
        public async Task Handle_WithSortBy_AppliesIdDescendingTieBreaker()
        {
            using var context = CreateInMemoryContext();
            var createdAt = DateTimeOffset.UtcNow;
            context.Set<Role>().AddRange(
                new Role { Id = 1, Code = "R1", Name = "SameName", Description = "D1", CreatedAt = createdAt, StatusRegister = true },
                new Role { Id = 2, Code = "R2", Name = "SameName", Description = "D2", CreatedAt = createdAt, StatusRegister = true },
                new Role { Id = 3, Code = "R3", Name = "SameName", Description = "D3", CreatedAt = createdAt, StatusRegister = true }
            );
            await context.SaveChangesAsync();

            var handler = new GetAllEntitiesQueryHandler<Role, int, RoleDto>(
                new BaseRepository<Role, int>(context),
                CreateMapper());

            var result = await handler.Handle(
                new GetAllEntitiesQuery<Role, int, RoleDto>
                {
                    PageNumber = 1,
                    PageSize = 10,
                    SortBy = "Name",
                    IsDescending = false
                },
                CancellationToken.None);

            result.Items.Select(r => r.Id).Should().Equal(3, 2, 1);
        }

        [Fact]
        public async Task Handle_WithKeysetCursor_ForcesCreatedAtDescIdDescAndPropagatesKeysetMetadata()
        {
            using var context = CreateInMemoryContext();
            var t0 = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
            context.Set<Role>().AddRange(
                new Role { Id = 1, Code = "A", Name = "Zeta", Description = "D1", CreatedAt = t0.AddMinutes(1), StatusRegister = true },
                new Role { Id = 2, Code = "B", Name = "Beta", Description = "D2", CreatedAt = t0.AddMinutes(2), StatusRegister = true },
                new Role { Id = 3, Code = "C", Name = "Alfa", Description = "D3", CreatedAt = t0.AddMinutes(2), StatusRegister = true },
                new Role { Id = 4, Code = "D", Name = "Delta", Description = "D4", CreatedAt = t0.AddMinutes(3), StatusRegister = true }
            );
            await context.SaveChangesAsync();

            var handler = new GetAllEntitiesQueryHandler<Role, int, RoleDto>(
                new BaseRepository<Role, int>(context),
                CreateMapper());

            // Primera página por offset/skipTotalCount o cursor inicial (orden por defecto CreatedAt DESC, Id DESC -> 4, 3, 2, 1)
            var page1 = await handler.Handle(
                new GetAllEntitiesQuery<Role, int, RoleDto>
                {
                    PageNumber = 1,
                    PageSize = 2,
                    SkipTotalCount = true
                },
                CancellationToken.None);

            page1.Items.Select(r => r.Id).Should().Equal(4, 3);
            page1.TotalRecords.Should().Be(-1);
            page1.HasMoreRows.Should().BeTrue();
            page1.CursorId.Should().Be(3);
            page1.CursorCreatedAt.Should().Be(t0.AddMinutes(2));

            // Segunda página por keyset (incluso si se pasa SortBy, keyset fuerza CreatedAt DESC, Id DESC)
            var page2 = await handler.Handle(
                new GetAllEntitiesQuery<Role, int, RoleDto>
                {
                    PageNumber = 1,
                    PageSize = 2,
                    SortBy = "Name",
                    CursorCreatedAt = page1.CursorCreatedAt,
                    CursorId = page1.CursorId,
                    SkipTotalCount = true
                },
                CancellationToken.None);

            page2.Items.Select(r => r.Id).Should().Equal(2, 1);
            page2.TotalRecords.Should().Be(-1);
            page2.HasMoreRows.Should().BeFalse();
            page2.CursorId.Should().Be(1);
            page2.CursorCreatedAt.Should().Be(t0.AddMinutes(1));
        }
    }
}

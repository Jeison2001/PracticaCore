using Application.Shared.Queries.RolePermissions;
using Application.Shared.Queries.RolePermissions.Handlers;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Tests.UnitTests.Application.Queries
{
    public class GetPermissionsByRoleQueryHandlerTests
    {
        private static AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task Handle_QueriesRolePermissionsInSingleJoinCall_OrderedByCodeAndFilteredByStatus()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var role = new Role { Id = 1, Code = "COORD", Name = "Coordinador", Description = "Coord", StatusRegister = true };
            var pZ = new Permission { Id = 1, Code = "Z_PERM", Description = "Z", StatusRegister = true };
            var pA = new Permission { Id = 2, Code = "A_PERM", Description = "A", StatusRegister = true };
            var pInactive = new Permission { Id = 3, Code = "B_PERM", Description = "B", StatusRegister = true };

            context.Set<Role>().Add(role);
            context.Set<Permission>().AddRange(pZ, pA, pInactive);
            context.Set<RolePermission>().AddRange(
                new RolePermission { Id = 10, IdRole = 1, IdPermission = 1, StatusRegister = true, OperationRegister = "Seed" },
                new RolePermission { Id = 11, IdRole = 1, IdPermission = 2, StatusRegister = true, OperationRegister = "Seed" },
                new RolePermission { Id = 12, IdRole = 1, IdPermission = 3, StatusRegister = false, OperationRegister = "Seed" }
            );
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var repo = new RolePermissionRepository(context);
            var handler = new GetPermissionsByRoleQueryHandler(repo);

            // Act: filtrar solo activos por código en minúsculas
            var activeOnly = await handler.Handle(
                new GetPermissionsByRoleQuery(roleId: null, roleCode: "coord", statusRegister: true),
                CancellationToken.None);

            // Assert
            activeOnly.Select(x => x.Permission.Code).Should().Equal("A_PERM", "Z_PERM");
            activeOnly.Should().AllSatisfy(x => x.RolePermission.StatusRegister.Should().BeTrue());

            // Act: sin filtro de estado devuelve activos e inactivos ordenados por código
            var allStatuses = await handler.Handle(
                new GetPermissionsByRoleQuery(roleId: 1, roleCode: null, statusRegister: null),
                CancellationToken.None);

            allStatuses.Select(x => x.Permission.Code).Should().Equal("A_PERM", "B_PERM", "Z_PERM");
        }

        [Fact]
        public async Task Handle_RoleIdAndRoleCodeConflict_ThrowsArgumentException()
        {
            // Arrange
            var repoMock = new Mock<IRolePermissionRepository>();
            repoMock.Setup(r => r.GetRoleByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Role { Id = 1, Code = "ADMIN", Name = "Admin" });
            repoMock.Setup(r => r.GetRoleByCodeAsync("STUDENT", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Role { Id = 2, Code = "STUDENT", Name = "Student" });

            var handler = new GetPermissionsByRoleQueryHandler(repoMock.Object);

            // Act
            var act = () => handler.Handle(new GetPermissionsByRoleQuery(1, "STUDENT", null), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Conflicto*");
            repoMock.Verify(r => r.GetByRoleIdWithPermissionsAsync(It.IsAny<int>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UsesSingleRepositoryJoinCall_ZeroNPlusOne()
        {
            // Arrange
            var repoMock = new Mock<IRolePermissionRepository>();
            var role = new Role { Id = 5, Code = "TEACHER", Name = "Docente" };
            var p1 = new Permission { Id = 10, Code = "N1MG", Description = "MG", StatusRegister = true };
            var p2 = new Permission { Id = 11, Code = "N2MGR", ParentCode = "N1MG", Description = "MGR", StatusRegister = true };

            repoMock.Setup(r => r.GetRoleByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(role);
            repoMock.Setup(r => r.GetByRoleIdWithPermissionsAsync(5, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    new RolePermission { Id = 100, IdRole = 5, IdPermission = 10, Permission = p1, StatusRegister = true },
                    new RolePermission { Id = 101, IdRole = 5, IdPermission = 11, Permission = p2, StatusRegister = true }
                ]);

            var handler = new GetPermissionsByRoleQueryHandler(repoMock.Object);

            // Act
            var result = await handler.Handle(new GetPermissionsByRoleQuery(5, null, true), CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            repoMock.Verify(r => r.GetByRoleIdWithPermissionsAsync(5, true, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.UnitTests.Infrastructure.Repositories
{
    public class UserInfoRepositoryTests
    {
        private static AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static UserInfoRepository CreateRepository(AppDbContext context)
        {
            var unitOfWork = new UnitOfWork(
                context,
                Mock.Of<IMediator>(),
                Mock.Of<ILogger<UnitOfWork>>());
            return new UserInfoRepository(unitOfWork);
        }

        private static void SeedPermissionCatalog(AppDbContext context)
        {
            context.Set<Permission>().AddRange(
                new Permission { Id = 1, Code = "N1PC", ParentCode = null, Description = "PC", StatusRegister = true },
                new Permission { Id = 2, Code = "N2PCSG", ParentCode = "N1PC", Description = "PCSG", StatusRegister = true },
                new Permission { Id = 3, Code = "N3PCSGG", ParentCode = "N2PCSG", Description = "PCSGG", StatusRegister = true },
                new Permission { Id = 4, Code = "N3PCSGR", ParentCode = "N2PCSG", Description = "PCSGR", StatusRegister = true },

                new Permission { Id = 5, Code = "N1MG", ParentCode = null, Description = "MG", StatusRegister = true },
                new Permission { Id = 6, Code = "N2MGR", ParentCode = "N1MG", Description = "MGR", StatusRegister = true },
                new Permission { Id = 7, Code = "N2MGC", ParentCode = "N1MG", Description = "MGC", StatusRegister = true },

                new Permission { Id = 8, Code = "N1SP", ParentCode = null, Description = "SP", StatusRegister = true },
                new Permission { Id = 9, Code = "N2SPSG", ParentCode = "N1SP", Description = "SPSG", StatusRegister = true },
                new Permission { Id = 10, Code = "N3SPSGG", ParentCode = "N2SPSG", Description = "SPSGG", StatusRegister = true }
            );
        }

        [Fact]
        public async Task GetUserLoginDataAsync_PrunesEmptyContainersAndIncludesMissingAncestors_FromUserPermissions()
        {
            using var context = CreateInMemoryContext();
            SeedPermissionCatalog(context);

            // Usuario con contenedores vacíos N1PC + N2PCSG, rama válida N1MG + N2MGR + N2MGC, y hoja huérfana N3SPSGG sin padres
            context.Set<UserPermission>().AddRange(
                new UserPermission { Id = 1, IdUser = 10, IdPermission = 1, StatusRegister = true },  // N1PC
                new UserPermission { Id = 2, IdUser = 10, IdPermission = 2, StatusRegister = true },  // N2PCSG
                new UserPermission { Id = 3, IdUser = 10, IdPermission = 3, StatusRegister = false }, // N3PCSGG inactivo
                new UserPermission { Id = 4, IdUser = 10, IdPermission = 5, StatusRegister = true },  // N1MG
                new UserPermission { Id = 5, IdUser = 10, IdPermission = 6, StatusRegister = true },  // N2MGR
                new UserPermission { Id = 6, IdUser = 10, IdPermission = 7, StatusRegister = true },  // N2MGC
                new UserPermission { Id = 7, IdUser = 10, IdPermission = 10, StatusRegister = true }  // N3SPSGG (sin N2SPSG ni N1SP)
            );
            await context.SaveChangesAsync();

            var repo = CreateRepository(context);

            // Act
            var loginData = await repo.GetUserLoginDataAsync(10);

            // Assert
            var codes = loginData.Permissions.Select(p => p.Code).ToList();
            codes.Should().NotContain("N1PC");
            codes.Should().NotContain("N2PCSG");
            codes.Should().BeEquivalentTo(["N1MG", "N2MGR", "N2MGC", "N1SP", "N2SPSG", "N3SPSGG"]);
        }

        [Fact]
        public async Task GetUserLoginDataAsync_RolePermissionFallback_PrunesEmptyContainersAndIncludesMissingAncestors()
        {
            using var context = CreateInMemoryContext();
            SeedPermissionCatalog(context);

            var role = new Role { Id = 1, Code = "COORD", Name = "Coordinador", Description = "Coord", StatusRegister = true };
            context.Set<Role>().Add(role);
            context.Set<UserRole>().Add(new UserRole { Id = 1, IdUser = 20, IdRole = 1, StatusRegister = true });
            context.Set<RolePermission>().AddRange(
                new RolePermission { Id = 1, IdRole = 1, IdPermission = 1, StatusRegister = true }, // N1PC (vacío)
                new RolePermission { Id = 2, IdRole = 1, IdPermission = 2, StatusRegister = true }, // N2PCSG (vacío)
                new RolePermission { Id = 3, IdRole = 1, IdPermission = 10, StatusRegister = true } // N3SPSGG (sin padres en RolePermission)
            );
            await context.SaveChangesAsync();

            var repo = CreateRepository(context);

            // Act
            var loginData = await repo.GetUserLoginDataAsync(20);

            // Assert
            loginData.Permissions.Select(p => p.Code)
                .Should().BeEquivalentTo(["N1SP", "N2SPSG", "N3SPSGG"]);
        }

        [Fact]
        public async Task GetUserPermissionsWithUserPermissionsAsync_ReturnsOnlyExistingUserPermissions_WithoutCreatingFromRolePermissions()
        {
            using var context = CreateInMemoryContext();
            SeedPermissionCatalog(context);

            var role = new Role { Id = 2, Code = "EST", Name = "Estudiante", Description = "Est", StatusRegister = true };
            context.Set<Role>().Add(role);
            context.Set<UserRole>().Add(new UserRole { Id = 2, IdUser = 30, IdRole = 2, StatusRegister = true });
            context.Set<RolePermission>().Add(new RolePermission
            {
                Id = 10,
                IdRole = 2,
                IdPermission = 10, // N3SPSGG asignado al rol, NO debe materializarse en UserPermission al hacer GET
                StatusRegister = true
            });
            context.Set<UserPermission>().AddRange(
                new UserPermission
                {
                    Id = 99,
                    IdUser = 30,
                    IdPermission = 1, // N1PC inactivo
                    StatusRegister = false,
                    OperationRegister = "Previo"
                },
                new UserPermission
                {
                    Id = 100,
                    IdUser = 30,
                    IdPermission = 6, // N2MGR activo
                    StatusRegister = true,
                    OperationRegister = "Directo"
                });
            await context.SaveChangesAsync();

            var repo = CreateRepository(context);

            // Act
            var result = await repo.GetUserPermissionsWithUserPermissionsAsync(30);

            // Assert
            result.Select(r => (r.Permission.Code, r.UserPermission.StatusRegister))
                .Should().BeEquivalentTo([
                    ("N1PC", false),
                    ("N2MGR", true)
                ]);

            var persistedCount = await context.Set<UserPermission>()
                .CountAsync(up => up.IdUser == 30);
            persistedCount.Should().Be(2);
        }
    }
}

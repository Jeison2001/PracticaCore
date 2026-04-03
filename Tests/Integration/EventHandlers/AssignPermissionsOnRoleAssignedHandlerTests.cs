using Application.Features.Security.EventHandlers;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tests.Integration.Utilities;
using Xunit;

namespace Tests.Integration.EventHandlers
{
    public class AssignPermissionsOnRoleAssignedHandlerTests : IntegrationTestBase
    {
        public AssignPermissionsOnRoleAssignedHandlerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Handle_UserRoleAssigned_AssignsConfiguredPermissions()
        {
            // Arrange
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Configurar Rol y Permisos
            var role = new Role { Id = 1, Code = "ROLE_TEST", Description = "Test Role", Name = "Test Role", StatusRegister = true, OperationRegister = "Test setup" };
            context.Set<Role>().Add(role);

            var perm1 = new Permission { Id = 1, Code = "PERM_1", Description = "Test", StatusRegister = true, OperationRegister = "Test setup" };
            var perm2 = new Permission { Id = 2, Code = "PERM_2", Description = "Test", StatusRegister = true, OperationRegister = "Test setup" };
            context.Set<Permission>().AddRange(perm1, perm2);

            var rolePerm1 = new RolePermission { Id = 1, IdRole = role.Id, IdPermission = perm1.Id, StatusRegister = true, OperationRegister = "Test setup" };
            var rolePerm2 = new RolePermission { Id = 2, IdRole = role.Id, IdPermission = perm2.Id, StatusRegister = true, OperationRegister = "Test setup" };
            context.Set<RolePermission>().AddRange(rolePerm1, rolePerm2);

            // Configurar Usuario
            var user = new User
            {
                Id = 1,
                Email = "testuser@test.edu.co",
                FirstName = "Test",
                LastName = "User",
                Identification = "123456",
                StatusRegister = true,
                OperationRegister = "Test setup"
            };
            context.Set<User>().Add(user);

            // Darle el rol inicialmente para justificar el evento
            var userRole = new UserRole
            {
                Id = 1,
                IdUser = user.Id,
                IdRole = role.Id,
                StatusRegister = true,
                OperationRegister = "Test setup"
            };
            context.Set<UserRole>().Add(userRole);

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            // Configurar Handler
            var handler = new AssignPermissionsOnRoleAssignedHandler(
                _scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
                _scope.ServiceProvider.GetRequiredService<ILogger<AssignPermissionsOnRoleAssignedHandler>>()
            );

            // Event
            var domainEvent = new UserRoleAssignedEvent(
                UserId: user.Id,
                RoleId: role.Id,
                TriggeredByUserId: 1
            );

            // Act
            await handler.Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert
            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

            var assignedPermissions = await actContext.Set<UserPermission>()
                .Where(x => x.IdUser == user.Id && x.StatusRegister)
                .Select(x => x.IdPermission)
                .ToListAsync();

            assignedPermissions.Should().HaveCount(2);
            assignedPermissions.Should().Contain(perm1.Id);
            assignedPermissions.Should().Contain(perm2.Id);
        }

        [Fact]
        public async Task Handle_UserRoleAssigned_IsIdempotentAndDoesNotDuplicatePermissions()
        {
            // Arrange
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Configurar Rol y Permisos - usar IDs únicos para evitar conflictos con la primera prueba
            var role = new Role { Id = 20, Code = "ROLE_IDEM", Description = "Test", Name = "Test Role Idem", StatusRegister = true, OperationRegister = "Test setup" };
            context.Set<Role>().Add(role);

            var perm1 = new Permission { Id = 20, Code = "PERM_IDEM_1", Description = "Test", StatusRegister = true, OperationRegister = "Test setup" };
            context.Set<Permission>().Add(perm1);

            var rolePerm1 = new RolePermission { Id = 20, IdRole = role.Id, IdPermission = perm1.Id, StatusRegister = true, OperationRegister = "Test setup" };
            context.Set<RolePermission>().Add(rolePerm1);

            // Configurar Usuario
            var user = new User
            {
                Id = 20,
                Email = "testuser.idem@test.edu.co",
                FirstName = "Test",
                LastName = "Idem",
                Identification = "654321",
                StatusRegister = true,
                OperationRegister = "Test setup"
            };
            context.Set<User>().Add(user);

            // Darle el rol inicialmente para justificar el evento
            var userRole = new UserRole
            {
                Id = 20,
                IdUser = user.Id,
                IdRole = role.Id,
                StatusRegister = true,
                OperationRegister = "Test setup"
            };
            context.Set<UserRole>().Add(userRole);

            // *** Parte clave de esta prueba: El usuario YA tiene el permiso de ejecución/evento previo
            var existingUserPerm = new UserPermission
            {
                Id = 20,
                IdUser = user.Id,
                IdPermission = perm1.Id,
                StatusRegister = true,
                OperationRegister = "Existing"
            };
            context.Set<UserPermission>().Add(existingUserPerm);

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            // Configurar Handler
            var handler = new AssignPermissionsOnRoleAssignedHandler(
                _scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
                _scope.ServiceProvider.GetRequiredService<ILogger<AssignPermissionsOnRoleAssignedHandler>>()
            );

            // Event
            var domainEvent = new UserRoleAssignedEvent(
                UserId: user.Id,
                RoleId: role.Id,
                TriggeredByUserId: 1
            );

            // Act
            await handler.Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert
            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

            var assignedPermissions = await actContext.Set<UserPermission>()
                .Where(x => x.IdUser == user.Id && x.IdPermission == perm1.Id)
                .ToListAsync();

            // Debe seguir teniendo solo UN registro de permiso asignado, no 2
            assignedPermissions.Should().HaveCount(1);
        }
    }
}

using System.Linq.Expressions;
using Application.Features.Security.EventHandlers;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.UnitTests.Application.EventHandlers
{
    public class AssignPermissionsOnRoleAssignedHandlerTests
    {
        [Fact]
        public async Task Handle_ExpandsMissingActiveAncestorsReactivatesParentsAndQueriesUserPermissionsOnce()
        {
            // Arrange
            var rolePermRepo = new Mock<IRepository<RolePermission, int>>();
            var permRepo = new Mock<IRepository<Permission, int>>();
            var userPermRepo = new Mock<IRepository<UserPermission, int>>();
            var uow = new Mock<IUnitOfWork>();

            uow.Setup(u => u.GetRepository<RolePermission, int>()).Returns(rolePermRepo.Object);
            uow.Setup(u => u.GetRepository<Permission, int>()).Returns(permRepo.Object);
            uow.Setup(u => u.GetRepository<UserPermission, int>()).Returns(userPermRepo.Object);

            // El rol solo tiene asignado el permiso hoja N3SPSGG (Id = 3), sin sus padres N2SPSG (2) y N1SP (1)
            rolePermRepo.Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<RolePermission, bool>>>(),
                    null, null, null))
                .ReturnsAsync([
                    new RolePermission { Id = 1, IdRole = 5, IdPermission = 3, StatusRegister = true }
                ]);

            permRepo.Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Permission, bool>>>(),
                    null, null, null))
                .ReturnsAsync([
                    new Permission { Id = 1, Code = "N1SP", ParentCode = null, StatusRegister = true },
                    new Permission { Id = 2, Code = "N2SPSG", ParentCode = "N1SP", StatusRegister = true },
                    new Permission { Id = 3, Code = "N3SPSGG", ParentCode = "N2SPSG", StatusRegister = true },
                ]);

            var inactiveN1 = new UserPermission
            {
                Id = 70,
                IdUser = 42,
                IdPermission = 1,
                StatusRegister = false,
                OperationRegister = "Desactivado"
            };

            userPermRepo.Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<UserPermission, bool>>>(),
                    null, null, null))
                .ReturnsAsync([inactiveN1]);

            var added = new List<UserPermission>();
            userPermRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<UserPermission>>()))
                .Callback<IEnumerable<UserPermission>>(items => added.AddRange(items))
                .Returns(Task.CompletedTask);

            var handler = new AssignPermissionsOnRoleAssignedHandler(
                uow.Object,
                Mock.Of<ILogger<AssignPermissionsOnRoleAssignedHandler>>());

            // Act
            await handler.Handle(new UserRoleAssignedEvent(UserId: 42, RoleId: 5, TriggeredByUserId: 1), CancellationToken.None);

            // Assert: una única consulta de UserPermission (sin N+1)
            userPermRepo.Verify(r => r.GetAllAsync(
                It.IsAny<Expression<Func<UserPermission, bool>>>(),
                null, null, null), Times.Once);

            inactiveN1.StatusRegister.Should().BeTrue();
            inactiveN1.OperationRegister.Should().Contain("Reactivación por asignación de Rol ID: 5");
            userPermRepo.Verify(r => r.UpdateAsync(inactiveN1), Times.Once);

            added.Select(x => x.IdPermission).Should().BeEquivalentTo([2, 3]);
        }
    }
}

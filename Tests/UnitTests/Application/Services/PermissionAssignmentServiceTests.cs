using System.Linq.Expressions;
using Application.Features.Shared.Services;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace Tests.UnitTests.Application.Services
{
    public class PermissionAssignmentServiceTests
    {
        [Fact]
        public async Task AssignPermissionsToInscriptionUsersAsync_EscalatesAncestorsReactivatesParentsAndAvoidsNPlusOne()
        {
            // Arrange
            var uimRepo = new Mock<IRepository<UserInscriptionModality, int>>();
            var permRepo = new Mock<IRepository<Permission, int>>();
            var userPermRepo = new Mock<IRepository<UserPermission, int>>();
            var uow = new Mock<IUnitOfWork>();

            uow.Setup(u => u.GetRepository<UserInscriptionModality, int>()).Returns(uimRepo.Object);
            uow.Setup(u => u.GetRepository<Permission, int>()).Returns(permRepo.Object);
            uow.Setup(u => u.GetRepository<UserPermission, int>()).Returns(userPermRepo.Object);

            uimRepo.Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<UserInscriptionModality, bool>>>(),
                    null, null, null))
                .ReturnsAsync([
                    new UserInscriptionModality { Id = 1, IdInscriptionModality = 10, IdUser = 100, StatusRegister = true },
                    new UserInscriptionModality { Id = 2, IdInscriptionModality = 10, IdUser = 200, StatusRegister = true },
                ]);

            var catalog = new List<Permission>
            {
                new() { Id = 1, Code = "N1PG", ParentCode = null, StatusRegister = true },
                new() { Id = 2, Code = "N2PGP", ParentCode = "N1PG", StatusRegister = true },
                new() { Id = 3, Code = "N3PGRP", ParentCode = "N2PGP", StatusRegister = true },
                new() { Id = 4, Code = "N3PGCP", ParentCode = "N2PGP", StatusRegister = true },
            };

            permRepo.Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Permission, bool>>>(),
                    null, null, null))
                .ReturnsAsync(catalog);

            var inactiveParentForUser100 = new UserPermission
            {
                Id = 50,
                IdUser = 100,
                IdPermission = 1, // N1PG inactivo previamente
                StatusRegister = false,
                OperationRegister = "Desactivado"
            };
            var activeChildForUser100 = new UserPermission
            {
                Id = 51,
                IdUser = 100,
                IdPermission = 3, // N3PGRP ya activo
                StatusRegister = true,
                OperationRegister = "Previo"
            };

            userPermRepo.Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<UserPermission, bool>>>(),
                    null, null, null))
                .ReturnsAsync([inactiveParentForUser100, activeChildForUser100]);

            var addedRows = new List<UserPermission>();
            userPermRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<UserPermission>>()))
                .Callback<IEnumerable<UserPermission>>(items => addedRows.AddRange(items))
                .Returns(Task.CompletedTask);

            // Act: solo solicitamos hojas N3PGRP y N3PGCP (debe escalar a N2PGP y N1PG)
            await PermissionAssignmentService.AssignPermissionsToInscriptionUsersAsync(
                uow.Object,
                inscriptionModalityId: 10,
                permissionCodes: ["N3PGRP", "N3PGCP"],
                triggeredByUserId: 999);

            // Assert: consulta batch única (cero N+1)
            userPermRepo.Verify(r => r.GetAllAsync(
                It.IsAny<Expression<Func<UserPermission, bool>>>(),
                null, null, null), Times.Once);
            userPermRepo.Verify(r => r.AnyAsync(
                It.IsAny<Expression<Func<UserPermission, bool>>>(),
                It.IsAny<CancellationToken>()), Times.Never);

            // Padre N1PG inactivo del usuario 100 fue reactivado
            inactiveParentForUser100.StatusRegister.Should().BeTrue();
            inactiveParentForUser100.IdUserUpdatedAt.Should().Be(999);
            inactiveParentForUser100.UpdatedAt.Should().NotBeNull();
            userPermRepo.Verify(r => r.UpdateAsync(inactiveParentForUser100), Times.Once);

            // Usuario 100 recibe N2PGP (2) y N3PGCP (4); Usuario 200 recibe los 4 permisos (1,2,3,4)
            addedRows.Where(x => x.IdUser == 100).Select(x => x.IdPermission)
                .Should().BeEquivalentTo([2, 4]);
            addedRows.Where(x => x.IdUser == 200).Select(x => x.IdPermission)
                .Should().BeEquivalentTo([1, 2, 3, 4]);
        }
    }
}

using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.Features.Shared.Services
{
    /// <summary>
    /// Servicio de aplicación reutilizable para asignar permisos a los usuarios vinculados a una inscripción
    /// </summary>
    public static class PermissionAssignmentService
    {
        public static async Task AssignPermissionsToInscriptionUsersAsync(
            IUnitOfWork unitOfWork,
            int inscriptionModalityId,
            string[] permissionCodes,
            int triggeredByUserId,
            CancellationToken cancellationToken = default)
        {
            var userInscriptionModalityRepo = unitOfWork.GetRepository<UserInscriptionModality, int>();
            var permissionRepo = unitOfWork.GetRepository<Permission, int>();
            var userPermissionRepo = unitOfWork.GetRepository<UserPermission, int>();

            // 1. Obtener los usuarios vinculados a la inscripción
            var userIds = (await userInscriptionModalityRepo.GetAllAsync(
                    x => x.IdInscriptionModality == inscriptionModalityId && x.StatusRegister))
                .Select(x => x.IdUser)
                .ToList();

            if (userIds.Count == 0) return;

            // 2. Resolver los IDs de los permisos por código (filtrado en BD)
            var allPermissions = await permissionRepo.GetAllAsync(
                p => permissionCodes.Contains(p.Code) && p.StatusRegister);

            foreach (var userId in userIds)
            {
                foreach (var perm in allPermissions)
                {
                    // 3. Idempotencia: asignar solo si no existe ya
                    var exists = await userPermissionRepo.AnyAsync(
                        up => up.IdUser == userId && up.IdPermission == perm.Id, cancellationToken);

                    if (!exists)
                    {
                        await userPermissionRepo.AddAsync(new UserPermission
                        {
                            IdUser = userId,
                            IdPermission = perm.Id,
                            IdUserCreatedAt = triggeredByUserId,
                            OperationRegister = "Asignado automáticamente por DomainEvent",
                            StatusRegister = true,
                            CreatedAt = DateTimeOffset.UtcNow
                        });
                    }
                }
            }
        }
    }
}


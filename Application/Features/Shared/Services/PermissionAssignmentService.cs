using Domain.Common.Auth;
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
            if (permissionCodes == null || permissionCodes.Length == 0) return;

            cancellationToken.ThrowIfCancellationRequested();

            var userInscriptionModalityRepo = unitOfWork.GetRepository<UserInscriptionModality, int>();
            var permissionRepo = unitOfWork.GetRepository<Permission, int>();
            var userPermissionRepo = unitOfWork.GetRepository<UserPermission, int>();

            // 1. Obtener los usuarios vinculados a la inscripción
            var userIds = (await userInscriptionModalityRepo.GetAllAsync(
                    x => x.IdInscriptionModality == inscriptionModalityId && x.StatusRegister))
                .Select(x => x.IdUser)
                .Distinct()
                .ToList();

            if (userIds.Count == 0) return;

            // 2. Resolver permisos activos solicitados junto con sus ancestros activos
            var activePermissions = (await permissionRepo.GetAllAsync(p => p.StatusRegister)).ToList();
            var resolvedPermissions = PermissionTreeHelper.ResolveActiveWithAncestors(activePermissions, permissionCodes);
            if (resolvedPermissions.Count == 0) return;

            var resolvedPermissionIds = resolvedPermissions.Select(p => p.Id).ToList();

            // 3. Cargar en lote los UserPermission existentes para evitar consultas N+1
            var existingUserPermissions = (await userPermissionRepo.GetAllAsync(
                    up => userIds.Contains(up.IdUser) && resolvedPermissionIds.Contains(up.IdPermission)))
                .GroupBy(up => (up.IdUser, up.IdPermission))
                .ToDictionary(g => g.Key, g => g.First());

            var now = DateTimeOffset.UtcNow;
            var toAdd = new List<UserPermission>();

            foreach (var userId in userIds)
            {
                foreach (var perm in resolvedPermissions)
                {
                    if (!existingUserPermissions.TryGetValue((userId, perm.Id), out var existing))
                    {
                        toAdd.Add(new UserPermission
                        {
                            IdUser = userId,
                            IdPermission = perm.Id,
                            IdUserCreatedAt = triggeredByUserId,
                            OperationRegister = "Asignado automáticamente por DomainEvent",
                            StatusRegister = true,
                            CreatedAt = now
                        });
                    }
                    else if (!existing.StatusRegister)
                    {
                        existing.StatusRegister = true;
                        existing.UpdatedAt = now;
                        existing.IdUserUpdatedAt = triggeredByUserId;
                        existing.OperationRegister = "Reactivado automáticamente por DomainEvent";
                        await userPermissionRepo.UpdateAsync(existing);
                    }
                }
            }

            if (toAdd.Count > 0)
            {
                await userPermissionRepo.AddRangeAsync(toAdd);
            }
        }
    }
}


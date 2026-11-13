using Domain.Common.Auth;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Security.EventHandlers
{
    /// <summary>
    /// Cuando se asigna un nuevo rol a un usuario, asigna automáticamente todos los permisos
    /// del rol al usuario.
    /// </summary>
    public class AssignPermissionsOnRoleAssignedHandler : INotificationHandler<UserRoleAssignedEvent>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AssignPermissionsOnRoleAssignedHandler> _logger;

        public AssignPermissionsOnRoleAssignedHandler(IUnitOfWork unitOfWork, ILogger<AssignPermissionsOnRoleAssignedHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Handle(UserRoleAssignedEvent notification, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rolePermissionRepo = _unitOfWork.GetRepository<RolePermission, int>();
            var permissionRepo = _unitOfWork.GetRepository<Permission, int>();
            var userPermissionRepo = _unitOfWork.GetRepository<UserPermission, int>();

            // Obtener todos los permisos asociados al rol asignado
            var rolePermissions = (await rolePermissionRepo.GetAllAsync(
                x => x.IdRole == notification.RoleId && x.StatusRegister)).ToList();

            if (rolePermissions.Count == 0) return;

            var activePermissions = (await permissionRepo.GetAllAsync(p => p.StatusRegister)).ToList();
            var resolvedPermissions = PermissionTreeHelper.ResolveActiveWithAncestorsByIds(
                activePermissions,
                rolePermissions.Select(rp => rp.IdPermission));

            if (resolvedPermissions.Count == 0) return;

            var resolvedPermissionIds = resolvedPermissions.Select(p => p.Id).ToList();
            var existingByPermissionId = (await userPermissionRepo.GetAllAsync(
                    x => x.IdUser == notification.UserId && resolvedPermissionIds.Contains(x.IdPermission)))
                .GroupBy(x => x.IdPermission)
                .ToDictionary(g => g.Key, g => g.First());

            var now = DateTimeOffset.UtcNow;
            var operationRegister = $"Asignación automática por Rol ID: {notification.RoleId}";
            var toAdd = new List<UserPermission>();

            foreach (var perm in resolvedPermissions)
            {
                if (!existingByPermissionId.TryGetValue(perm.Id, out var existingUserPermission))
                {
                    toAdd.Add(new UserPermission
                    {
                        IdUser = notification.UserId,
                        IdPermission = perm.Id,
                        IdUserCreatedAt = notification.TriggeredByUserId,
                        OperationRegister = operationRegister,
                        StatusRegister = true,
                        CreatedAt = now
                    });
                }
                else if (!existingUserPermission.StatusRegister)
                {
                    // El nuevo rol asignado reactiva el permiso para el usuario
                    existingUserPermission.StatusRegister = true;
                    existingUserPermission.IdUserUpdatedAt = notification.TriggeredByUserId;
                    existingUserPermission.UpdatedAt = now;
                    existingUserPermission.OperationRegister = $"Reactivación por asignación de Rol ID: {notification.RoleId}";
                    await userPermissionRepo.UpdateAsync(existingUserPermission);
                }
            }

            if (toAdd.Count > 0)
            {
                await userPermissionRepo.AddRangeAsync(toAdd);
            }
        }
    }
}


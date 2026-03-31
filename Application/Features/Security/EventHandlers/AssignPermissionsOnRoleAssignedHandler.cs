using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Security.EventHandlers
{
    /// <summary>
    /// Reemplaza: TrgUserRoleAssignPermissions
    /// Cuando se asigna un nuevo rol a un usuario, asigna automáticamente todos los permisos
    /// del rol al usuario (equivalente al trigger AFTER INSERT en UserRole).
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
            var rolePermissionRepo = _unitOfWork.GetRepository<RolePermission, int>();
            var userPermissionRepo = _unitOfWork.GetRepository<UserPermission, int>();

            // Obtener todos los permisos asociados al rol asignado
            var rolePermissions = await rolePermissionRepo.GetAllAsync(
                x => x.IdRole == notification.RoleId && x.StatusRegister);

            if (!rolePermissions.Any()) return;

            var operationRegister = $"Asignación automática por Rol ID: {notification.RoleId}";

            foreach (var rolePermission in rolePermissions)
            {
                // Idempotencia: no duplicar si el permiso ya existe para el usuario
                var exists = await userPermissionRepo.AnyAsync(
                    x => x.IdUser == notification.UserId && x.IdPermission == rolePermission.IdPermission,
                    cancellationToken);

                if (!exists)
                {
                    await userPermissionRepo.AddAsync(new UserPermission
                    {
                        IdUser = notification.UserId,
                        IdPermission = rolePermission.IdPermission,
                        IdUserCreatedAt = notification.TriggeredByUserId,
                        OperationRegister = operationRegister,
                        StatusRegister = true,
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                }
            }
        }
    }
}


using Application.Shared.DTOs.Permissions;
using Application.Shared.DTOs.RolePermissions;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.RolePermissions.Handlers
{
    /// <summary>
    /// Obtiene los permisos asociados a un rol en una sola consulta con su relación Permission.
    /// </summary>
    public class GetPermissionsByRoleQueryHandler : IRequestHandler<GetPermissionsByRoleQuery, List<RolePermissionInfoDto>>
    {
        private readonly IRolePermissionRepository _rolePermissionRepository;

        public GetPermissionsByRoleQueryHandler(IRolePermissionRepository rolePermissionRepository)
        {
            _rolePermissionRepository = rolePermissionRepository;
        }

        public async Task<List<RolePermissionInfoDto>> Handle(GetPermissionsByRoleQuery request, CancellationToken cancellationToken)
        {
            if (!request.RoleId.HasValue && string.IsNullOrEmpty(request.RoleCode))
            {
                throw new ArgumentException("Debe proporcionar al menos el ID o el código del rol.");
            }

            Role? role = null;

            if (request.RoleId.HasValue && !string.IsNullOrEmpty(request.RoleCode))
            {
                var roleById = await _rolePermissionRepository.GetRoleByIdAsync(request.RoleId.Value, cancellationToken);
                var roleByCode = await _rolePermissionRepository.GetRoleByCodeAsync(request.RoleCode, cancellationToken);

                if (roleById != null && roleByCode != null && roleById.Id != roleByCode.Id)
                {
                    throw new ArgumentException($"Conflicto: El ID {request.RoleId} corresponde al rol '{roleById.Code}' pero el código proporcionado es '{request.RoleCode}'. Los parámetros deben referirse al mismo rol.");
                }

                role = roleById ?? roleByCode;
            }
            else if (request.RoleId.HasValue)
            {
                role = await _rolePermissionRepository.GetRoleByIdAsync(request.RoleId.Value, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(request.RoleCode))
            {
                role = await _rolePermissionRepository.GetRoleByCodeAsync(request.RoleCode, cancellationToken);
            }

            if (role == null)
            {
                return new List<RolePermissionInfoDto>();
            }

            var rolePermissions = await _rolePermissionRepository.GetByRoleIdWithPermissionsAsync(
                role.Id,
                request.StatusRegister,
                cancellationToken);

            return rolePermissions.Select(rp => new RolePermissionInfoDto
            {
                Permission = new PermissionDto
                {
                    Id = rp.Permission.Id,
                    Code = rp.Permission.Code,
                    ParentCode = rp.Permission.ParentCode,
                    Description = rp.Permission.Description,
                    IdUserCreatedAt = rp.Permission.IdUserCreatedAt,
                    IdUserUpdatedAt = rp.Permission.IdUserUpdatedAt,
                    UpdatedAt = rp.Permission.UpdatedAt,
                    StatusRegister = rp.Permission.StatusRegister,
                    OperationRegister = rp.Permission.OperationRegister
                },
                RolePermission = new RolePermissionDto
                {
                    Id = rp.Id,
                    IdRole = rp.IdRole,
                    IdPermission = rp.IdPermission,
                    IdUserCreatedAt = rp.IdUserCreatedAt,
                    IdUserUpdatedAt = rp.IdUserUpdatedAt,
                    UpdatedAt = rp.UpdatedAt,
                    StatusRegister = rp.StatusRegister,
                    OperationRegister = rp.OperationRegister
                }
            }).ToList();
        }
    }
}
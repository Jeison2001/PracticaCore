using Application.Shared.DTOs.Permission;
using Application.Shared.DTOs.RolePermission;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries.RolePermission.Handlers
{
    public class GetPermissionsByRoleQueryHandler : IRequestHandler<GetPermissionsByRoleQuery, List<RolePermissionInfoDto>>
    {
        private readonly IRepository<Domain.Entities.Role, int> _roleRepository;
        private readonly IRepository<Domain.Entities.RolePermission, int> _rolePermissionRepository;
        private readonly IRepository<Domain.Entities.Permission, int> _permissionRepository;

        public GetPermissionsByRoleQueryHandler(
            IRepository<Domain.Entities.Role, int> roleRepository,
            IRepository<Domain.Entities.RolePermission, int> rolePermissionRepository,
            IRepository<Domain.Entities.Permission, int> permissionRepository)
        {
            _roleRepository = roleRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _permissionRepository = permissionRepository;
        }        public async Task<List<RolePermissionInfoDto>> Handle(GetPermissionsByRoleQuery request, CancellationToken cancellationToken)
        {
            // Validar que al menos uno de los parámetros esté presente
            if (!request.RoleId.HasValue && string.IsNullOrEmpty(request.RoleCode))
            {
                throw new ArgumentException("Debe proporcionar al menos el ID o el código del rol.");
            }

            Domain.Entities.Role? role = null;

            // Si ambos parámetros están presentes, validar que correspondan al mismo rol
            if (request.RoleId.HasValue && !string.IsNullOrEmpty(request.RoleCode))
            {
                var roleById = await _roleRepository.GetByIdAsync(request.RoleId.Value);
                var roleByCode = await _roleRepository.GetFirstOrDefaultAsync(
                    r => r.Code.ToLower() == request.RoleCode.ToLower(), 
                    cancellationToken);

                if (roleById != null && roleByCode != null && roleById.Id != roleByCode.Id)
                {
                    throw new ArgumentException($"Conflicto: El ID {request.RoleId} corresponde al rol '{roleById.Code}' pero el código proporcionado es '{request.RoleCode}'. Los parámetros deben referirse al mismo rol.");
                }

                // Usar el rol encontrado por ID (tiene precedencia si ambos existen)
                role = roleById ?? roleByCode;
            }
            // Buscar el rol por ID o por código
            else if (request.RoleId.HasValue)
            {
                role = await _roleRepository.GetByIdAsync(request.RoleId.Value);
            }
            else if (!string.IsNullOrEmpty(request.RoleCode))
            {
                role = await _roleRepository.GetFirstOrDefaultAsync(
                    r => r.Code.ToLower() == request.RoleCode.ToLower(), 
                    cancellationToken);
            }

            if (role == null)
            {
                return new List<RolePermissionInfoDto>();
            }            // Obtener todos los RolePermissions para el rol encontrado
            var rolePermissions = await _rolePermissionRepository.GetAllAsync(
                rp => rp.IdRole == role.Id && rp.StatusRegister);

            var result = new List<RolePermissionInfoDto>();

            foreach (var rolePermission in rolePermissions)
            {
                // Obtener la información del permiso
                var permission = await _permissionRepository.GetByIdAsync(rolePermission.IdPermission);
                
                if (permission != null)
                {
                    result.Add(new RolePermissionInfoDto
                    {
                        Permission = new PermissionDto
                        {
                            Id = permission.Id,
                            Code = permission.Code,
                            ParentCode = permission.ParentCode,
                            Description = permission.Description,
                            IdUserCreatedAt = permission.IdUserCreatedAt,
                            IdUserUpdatedAt = permission.IdUserUpdatedAt,
                            UpdatedAt = permission.UpdatedAt,
                            StatusRegister = permission.StatusRegister,
                            OperationRegister = permission.OperationRegister
                        },
                        RolePermission = new RolePermissionDto
                        {
                            Id = rolePermission.Id,
                            IdRole = rolePermission.IdRole,
                            IdPermission = rolePermission.IdPermission,
                            IdUserCreatedAt = rolePermission.IdUserCreatedAt,
                            IdUserUpdatedAt = rolePermission.IdUserUpdatedAt,
                            UpdatedAt = rolePermission.UpdatedAt,
                            StatusRegister = rolePermission.StatusRegister,
                            OperationRegister = rolePermission.OperationRegister
                        }
                    });
                }
            }

            return result;
        }
    }
}

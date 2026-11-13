using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Repositories
{
    /// <summary>
    /// Repositorio especializado para consultas de permisos por rol.
    /// </summary>
    public interface IRolePermissionRepository : IScopedService
    {
        /// <summary>
        /// Obtiene un rol por su identificador sin seguimiento de cambios.
        /// </summary>
        Task<Role?> GetRoleByIdAsync(int roleId, CancellationToken ct = default);

        /// <summary>
        /// Obtiene un rol por su código (insensible a mayúsculas) sin seguimiento de cambios.
        /// </summary>
        Task<Role?> GetRoleByCodeAsync(string roleCode, CancellationToken ct = default);

        /// <summary>
        /// Obtiene los registros RolePermission junto con su Permission asociada para un rol,
        /// filtrados opcionalmente por estado y ordenados por código de permiso.
        /// </summary>
        Task<List<RolePermission>> GetByRoleIdWithPermissionsAsync(
            int roleId,
            bool? statusRegister,
            CancellationToken ct = default);
    }
}

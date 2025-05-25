using Application.Shared.DTOs.Permission;

namespace Application.Shared.DTOs.RolePermission
{
    /// <summary>
    /// DTO que representa la información de permisos de un rol con los datos de la relación RolePermission
    /// </summary>
    public class RolePermissionInfoDto
    {
        /// <summary>
        /// Información completa del permiso
        /// </summary>
        public PermissionDto Permission { get; set; } = new();

        /// <summary>
        /// Información del registro de relación RolePermission
        /// </summary>
        public RolePermissionDto RolePermission { get; set; } = new();
    }
}

using Application.Shared.DTOs.Permissions;

namespace Application.Shared.DTOs.UserPermissions
{
    /// <summary>
    /// DTO que representa la información de permisos de un usuario con los datos de la relación UserPermission
    /// </summary>
    public record UserPermissionInfoDto
    {
        /// <summary>
        /// Información completa del permiso
        /// </summary>
        public PermissionDto Permission { get; set; } = new();

        /// <summary>
        /// Información del registro de relación UserPermission
        /// </summary>
        public UserPermissionDto UserPermission { get; set; } = new();
    }
}

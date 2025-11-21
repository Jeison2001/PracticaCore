using Application.Shared.DTOs.Roles;

namespace Application.Shared.DTOs.UserRoles
{
    /// <summary>
    /// DTO que representa la información de rol de un usuario con los datos de la relación UserRole
    /// </summary>
    public record UserRoleInfoDto
    {
        /// <summary>
        /// Información completa del rol
        /// </summary>
        public RoleDto Role { get; set; } = new();
          /// <summary>
        /// Información del registro de relación UserRole
        /// </summary>
        public UserRoleDto UserRole { get; set; } = new();
    }
}

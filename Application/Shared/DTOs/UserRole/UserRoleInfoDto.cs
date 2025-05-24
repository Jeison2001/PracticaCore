using Application.Shared.DTOs.Role;

namespace Application.Shared.DTOs.UserRole
{
    /// <summary>
    /// DTO que representa la información de rol de un usuario, manteniendo una estructura familiar
    /// pero incluyendo los datos necesarios del registro de relación UserRole para operaciones de gestión
    /// </summary>
    public class UserRoleInfoDto : RoleDto
    {
        /// <summary>
        /// ID del registro UserRole (necesario para operaciones como desactivación)
        /// </summary>
        public int UserRoleId { get; set; }
        
        /// <summary>
        /// ID del usuario al que pertenece este rol
        /// </summary>
        public int UserId { get; set; }
        
        /// <summary>
        /// Fecha de asignación del rol (tomada del UserRole)
        /// </summary>
        public new DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Usuario que asignó este rol (tomado del UserRole)
        /// </summary>
        public new int? IdUserCreatedAt { get; set; }
        
        /// <summary>
        /// Estado del registro de la relación UserRole
        /// </summary>
        public new bool StatusRegister { get; set; }
        
        /// <summary>
        /// Operación realizada en el registro UserRole
        /// </summary>
        public new string OperationRegister { get; set; } = string.Empty;
    }
}

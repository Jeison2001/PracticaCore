using Domain.Common;

namespace Domain.Events
{
    /// <summary>
    /// Se dispara cuando se asigna un nuevo rol a un usuario.
    /// Escucha sobre la tabla UserRole (INSERT).
    /// </summary>
    public record UserRoleAssignedEvent(
        int UserId,
        int RoleId,
        int? TriggeredByUserId) : BaseEvent;
}

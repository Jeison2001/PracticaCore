using Domain.Entities;

namespace Domain.Common.Auth;

/// <summary>
/// Tupla que combina un Permission con su UserPermission asociada para un usuario específico.
/// Elimina la dependencia de lazy loading.
/// </summary>
public record PermissionWithUserPermission(Permission Permission, UserPermission UserPermission);

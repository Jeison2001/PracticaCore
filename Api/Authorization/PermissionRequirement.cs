using Microsoft.AspNetCore.Authorization;

namespace Api.Authorization
{
    /// <summary>
    /// Requisito de autorización que valida la presencia de al menos uno de los códigos de permiso requeridos.
    /// </summary>
    public sealed class PermissionRequirement : IAuthorizationRequirement
    {
        public IReadOnlyCollection<string> RequiredPermissions { get; }

        public PermissionRequirement(IReadOnlyCollection<string> requiredPermissions)
        {
            if (requiredPermissions == null || requiredPermissions.Count == 0)
            {
                throw new ArgumentException("Debe proporcionar al menos un código de permiso.", nameof(requiredPermissions));
            }

            RequiredPermissions = requiredPermissions;
        }
    }
}

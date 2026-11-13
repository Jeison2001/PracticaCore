using Microsoft.AspNetCore.Authorization;

namespace Api.Authorization
{
    /// <summary>
    /// Exige que el usuario autenticado posea al menos uno de los códigos de permiso indicados.
    /// Múltiples atributos sobre una acción o controlador se evalúan con semántica AND.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class RequirePermissionAttribute : AuthorizeAttribute
    {
        public const string PolicyPrefix = "Perm:";

        public IReadOnlyCollection<string> PermissionCodes { get; }

        public RequirePermissionAttribute(params string[] permissionCodes)
        {
            if (permissionCodes == null)
            {
                throw new ArgumentException("Debe proporcionar al menos un código de permiso.", nameof(permissionCodes));
            }

            var normalizedCodes = permissionCodes
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (normalizedCodes.Length == 0)
            {
                throw new ArgumentException("Debe proporcionar al menos un código de permiso válido.", nameof(permissionCodes));
            }

            PermissionCodes = normalizedCodes;
            Policy = $"{PolicyPrefix}{string.Join(',', normalizedCodes)}";
        }
    }
}

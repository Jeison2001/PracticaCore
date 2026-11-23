using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services.Auth
{
    internal static class AuthValidationHelper
    {
        private static readonly string[] DefaultAllowedDomains = ["unicesar.edu.co"];

        public static string[] GetAllowedDomains(IConfiguration? configuration)
        {
            var globalDomains = ReadDomainsFromSection(configuration, "Authentication:AllowedDomains");
            if (globalDomains.Length > 0)
                return globalDomains;

            var registerDomains = ReadDomainsFromSection(configuration, "Authentication:Register:AllowedDomains");
            if (registerDomains.Length > 0)
                return registerDomains;

            return DefaultAllowedDomains;
        }

        private static string[] ReadDomainsFromSection(IConfiguration? configuration, string sectionPath)
        {
            if (configuration == null)
                return Array.Empty<string>();

            var section = configuration.GetSection(sectionPath);
            var childrenValues = section.GetChildren()
                .SelectMany(c => (c.Value ?? string.Empty).Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(v => v.TrimStart('@'))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (childrenValues.Length > 0)
                return childrenValues;

            if (!string.IsNullOrWhiteSpace(section.Value))
            {
                return section.Value
                    .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(v => v.TrimStart('@'))
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            return Array.Empty<string>();
        }

        public static void EnsureEmailDomainAllowed(string? email, IEnumerable<string>? allowedDomains)
        {
            if (allowedDomains == null)
                return;

            var validDomains = allowedDomains
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => d.Trim().TrimStart('@'))
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .ToArray();

            if (validDomains.Length == 0)
                return;

            var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
            var atIndex = normalizedEmail.LastIndexOf('@');
            if (atIndex <= 0 || atIndex == normalizedEmail.Length - 1)
                throw new UnauthorizedAccessException("El dominio de correo no está autorizado para acceder al sistema.");

            var domain = normalizedEmail[(atIndex + 1)..];
            if (!validDomains.Any(allowed => string.Equals(allowed, domain, StringComparison.OrdinalIgnoreCase)))
                throw new UnauthorizedAccessException("El dominio de correo no está autorizado para acceder al sistema.");
        }
    }
}

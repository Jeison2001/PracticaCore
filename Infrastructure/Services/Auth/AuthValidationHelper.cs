using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services.Auth
{
    public static class AuthValidationHelper
    {
        private static readonly string[] DefaultAllowedDomains = ["unicesar.edu.co"];

        public static string[] GetAllowedDomains(IConfiguration? configuration) =>
            ResolveAllowedDomains(configuration, "Authentication:AllowedDomains", "Authentication:Register:AllowedDomains");

        public static string[] GetAllowedRegisterDomains(IConfiguration? configuration) =>
            ResolveAllowedDomains(configuration, "Authentication:Register:AllowedDomains", "Authentication:AllowedDomains");

        private static string[] ResolveAllowedDomains(IConfiguration? configuration, string primaryKey, string fallbackKey)
        {
            var primary = ReadDomainsFromSection(configuration, primaryKey);
            if (primary.Length > 0)
                return primary;

            var fallback = ReadDomainsFromSection(configuration, fallbackKey);
            return fallback.Length > 0 ? fallback : DefaultAllowedDomains;
        }

        private static string[] ReadDomainsFromSection(IConfiguration? configuration, string sectionPath)
        {
            if (configuration == null)
                return Array.Empty<string>();

            var section = configuration.GetSection(sectionPath);
            if (!string.IsNullOrWhiteSpace(section.Value))
            {
                var flatValues = NormalizeDomainEntries([section.Value]);
                if (flatValues.Length > 0)
                    return flatValues;
            }

            return NormalizeDomainEntries(section.GetChildren().Select(c => c.Value));
        }

        private static string[] NormalizeDomainEntries(IEnumerable<string?> entries) =>
            entries
                .SelectMany(entry => (entry ?? string.Empty).Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(v => v.Trim().TrimStart('@').ToLowerInvariant())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

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

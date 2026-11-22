using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services.Auth
{
    internal static class AuthValidationHelper
    {
        private static readonly string[] DefaultAllowedDomains = ["unicesar.edu.co"];

        public static string[] GetAllowedDomains(IConfiguration? configuration, string? provider = null)
        {
            var globalDomains = ReadDomainsFromSection(configuration, "Authentication:AllowedDomains");
            if (globalDomains.Length > 0)
                return globalDomains;

            var providerDomains = GetProviderAllowedDomains(configuration, provider);
            if (providerDomains.Length > 0)
                return providerDomains;

            var registerDomains = ReadDomainsFromSection(configuration, "Authentication:Register:AllowedDomains");
            if (registerDomains.Length > 0)
                return registerDomains;

            return DefaultAllowedDomains;
        }

        private static string[] GetProviderAllowedDomains(IConfiguration? configuration, string? provider)
        {
            if (configuration == null || string.IsNullOrWhiteSpace(provider))
                return Array.Empty<string>();

            var normalizedProvider = provider.Trim().ToLowerInvariant();
            var sectionName = normalizedProvider switch
            {
                "google" => "Google",
                "azure" => "AzureAd",
                _ => provider.Trim()
            };

            var allowedDomains = ReadDomainsFromSection(configuration, $"Authentication:{sectionName}:AllowedDomains");
            if (allowedDomains.Length > 0)
                return allowedDomains;

            if (normalizedProvider == "google")
            {
                var hostedDomain = configuration["Authentication:Google:HostedDomain"]?.Trim().TrimStart('@');
                if (!string.IsNullOrWhiteSpace(hostedDomain) && hostedDomain != "*")
                    return [hostedDomain];
            }

            return Array.Empty<string>();
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Api.Authorization
{
    /// <summary>
    /// Proveedor dinámico de políticas para nombres con prefijo <see cref="RequirePermissionAttribute.PolicyPrefix"/>.
    /// Delega el resto de políticas en <see cref="DefaultAuthorizationPolicyProvider"/>.
    /// </summary>
    public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
            => _fallbackPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
            => _fallbackPolicyProvider.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (!string.IsNullOrEmpty(policyName) &&
                policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
            {
                var rawCodes = policyName.Substring(RequirePermissionAttribute.PolicyPrefix.Length);
                var codes = rawCodes
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                if (codes.Length > 0)
                {
                    var policy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .AddRequirements(new PermissionRequirement(codes))
                        .Build();

                    return Task.FromResult<AuthorizationPolicy?>(policy);
                }
            }

            return _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }
    }
}

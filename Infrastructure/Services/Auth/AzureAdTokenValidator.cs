using Domain.Common.Auth;
using Domain.Configuration;
using Domain.Interfaces.Services.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services.Auth
{
    /// <summary>
    /// Valida id_tokens de Microsoft Entra ID (Azure AD) contra el documento OIDC del tenant
    /// y mapea los claims al DTO de dominio.
    /// </summary>
    public class AzureAdTokenValidator : ITokenValidator
    {
        private readonly AzureAdAuthOptions _options;
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager;

        public AzureAdTokenValidator(IOptions<AzureAdAuthOptions> options)
        {
            _options = options.Value;

            if (string.IsNullOrEmpty(_options.TenantId))
                throw new InvalidOperationException("Authentication:AzureAd:TenantId no está configurado.");
            if (string.IsNullOrEmpty(_options.ClientId))
                throw new InvalidOperationException("Authentication:AzureAd:ClientId no está configurado.");

            var instance = _options.Instance.TrimEnd('/');
            var metadataAddress = $"{instance}/{_options.TenantId}/v2.0/.well-known/openid-configuration";

            _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever());
        }

        public string Provider => "azure";

        public async Task<TokenPayload> ValidateAsync(string idToken)
        {
            var openIdConfig = await _configManager.GetConfigurationAsync(CancellationToken.None);

            // En endpoints multi-tenant el issuer del token varía por tenant emisor.
            var multiTenant = _options.TenantId.Equals("common", StringComparison.OrdinalIgnoreCase)
                || _options.TenantId.Equals("organizations", StringComparison.OrdinalIgnoreCase)
                || _options.TenantId.Equals("consumers", StringComparison.OrdinalIgnoreCase);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudience = _options.ClientId,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = openIdConfig.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            if (multiTenant)
            {
                // Acepta cualquier issuer del esquema de Microsoft.
                validationParameters.IssuerValidator = (issuer, _, _) =>
                {
                    if (issuer.StartsWith("https://login.microsoftonline.com/", StringComparison.OrdinalIgnoreCase)
                        || issuer.StartsWith("https://sts.windows.net/", StringComparison.OrdinalIgnoreCase)
                        || issuer.StartsWith("https://login.live.com", StringComparison.OrdinalIgnoreCase))
                    {
                        return issuer;
                    }
                    throw new SecurityTokenInvalidIssuerException($"Issuer no permitido: {issuer}");
                };
            }
            else
            {
                validationParameters.ValidIssuer = openIdConfig.Issuer;
            }

            var handler = new JsonWebTokenHandler();
            var result = await handler.ValidateTokenAsync(idToken, validationParameters);

            if (!result.IsValid)
            {
                throw new UnauthorizedAccessException(
                    "Token de Azure AD inválido: " + (result.Exception?.Message ?? "validación fallida"));
            }

            var claims = result.Claims;

            string GetClaim(params string[] keys)
            {
                foreach (var key in keys)
                {
                    if (claims.TryGetValue(key, out var value) && value is not null)
                    {
                        return value.ToString() ?? string.Empty;
                    }
                }
                return string.Empty;
            }

            return new TokenPayload
            {
                Email = GetClaim("email", "preferred_username", "upn"),
                GivenName = GetClaim("given_name"),
                FamilyName = GetClaim("family_name")
            };
        }
    }
}

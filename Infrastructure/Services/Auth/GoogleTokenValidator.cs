using Domain.Common.Auth;
using Domain.Configuration;
using Domain.Interfaces.Services.Auth;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Auth
{
    /// <summary>
    /// Validación de tokens usando Google Sign-In.
    /// Valida la firma del id_token y que su audience coincida con el ClientId configurado.
    /// Adapta la respuesta de Google al DTO de dominio.
    /// </summary>
    public class GoogleTokenValidator : ITokenValidator
    {
        private readonly GoogleAuthOptions _options;
        private readonly Func<string, GoogleJsonWebSignature.ValidationSettings, Task<GoogleJsonWebSignature.Payload>> _validateToken;

        public GoogleTokenValidator(
            IOptions<GoogleAuthOptions> options,
            Func<string, GoogleJsonWebSignature.ValidationSettings, Task<GoogleJsonWebSignature.Payload>>? validateToken = null)
        {
            _options = options.Value;
            _validateToken = validateToken ?? GoogleJsonWebSignature.ValidateAsync;
        }

        public string Provider => "google";

        public async Task<TokenPayload> ValidateAsync(string idToken)
        {
            if (string.IsNullOrWhiteSpace(_options.ClientId))
            {
                throw new InvalidOperationException("Google ClientId no está configurado.");
            }

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _options.ClientId }
            };

            if (!string.IsNullOrEmpty(_options.HostedDomain))
            {
                settings.HostedDomain = _options.HostedDomain;
            }

            var payload = await _validateToken(idToken, settings);

            if (!payload.EmailVerified)
            {
                throw new UnauthorizedAccessException("El correo de Google no está verificado.");
            }

            AuthValidationHelper.EnsureEmailDomainAllowed(payload.Email, _options.AllowedDomains);

            return new TokenPayload
            {
                Email = payload.Email,
                GivenName = payload.GivenName,
                FamilyName = payload.FamilyName
            };
        }
    }
}

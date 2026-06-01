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

        public GoogleTokenValidator(IOptions<GoogleAuthOptions> options)
        {
            _options = options.Value;
        }

        public string Provider => "google";

        public async Task<TokenPayload> ValidateAsync(string idToken)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings();

            if (!string.IsNullOrEmpty(_options.ClientId))
            {
                settings.Audience = new[] { _options.ClientId };
            }

            if (!string.IsNullOrEmpty(_options.HostedDomain))
            {
                settings.HostedDomain = _options.HostedDomain;
            }

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return new TokenPayload
            {
                Email = payload.Email,
                GivenName = payload.GivenName,
                FamilyName = payload.FamilyName
            };
        }
    }
}

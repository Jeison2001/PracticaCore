using Domain.Common.Auth;
using Domain.Interfaces.Services.Auth;
using Google.Apis.Auth;

namespace Infrastructure.Services.Auth
{
    /// <summary>
    /// Implementación de validación de tokens usando Google Sign-In.
    /// Adapta la respuesta de Google al DTO de dominio.
    /// </summary>
    public class GoogleTokenValidator : ITokenValidator
    {
        public async Task<TokenPayload> ValidateAsync(string idToken)
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
            
            return new TokenPayload
            {
                Email = payload.Email,
                GivenName = payload.GivenName,
                FamilyName = payload.FamilyName
            };
        }
    }
}

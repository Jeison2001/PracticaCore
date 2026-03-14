using Domain.Common.Auth;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Auth
{
    public interface IAuthService : IScopedService
    {
        /// <summary>
        /// Autentica a un usuario usando un token de Google.
        /// </summary>
        /// <param name="idToken">El token ID de Google</param>
        /// <returns>Resultado de la autenticación con información del usuario y token</returns>
        Task<AuthenticationResult> AuthenticateWithGoogleAsync(string idToken);
    }
}
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

        /// <summary>
        /// Autentica a un usuario usando email y password (para testing E2E).
        /// </summary>
        /// <param name="email">Email del usuario</param>
        /// <param name="password">Password (usar E2E_TEST_SECRET_2026 para testing)</param>
        /// <returns>Resultado de la autenticación con información del usuario y token</returns>
        Task<AuthenticationResult> AuthenticateManualAsync(string email, string? password);
    }
}
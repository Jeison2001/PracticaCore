using Domain.Common.Auth;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Auth
{
    /// <summary>
    /// Servicio de autenticación manual (email) para pruebas E2E.
    /// Separado de IAuthService porque no es un proveedor de identidad federado.
    /// NO usar en producción.
    /// </summary>
    public interface IManualAuthService : IScopedService
    {
        /// <summary>
        /// Autentica a un usuario usando email (para testing E2E).
        /// </summary>
        /// <param name="email">Email del usuario</param>
        /// <param name="password">Password opcional</param>
        /// <returns>Resultado de la autenticación con información del usuario y token</returns>
        Task<AuthenticationResult> AuthenticateManualAsync(string email, string? password);
    }
}

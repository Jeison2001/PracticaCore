using Domain.Common.Auth;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Auth
{
    /// <summary>
    /// Servicio de autenticación federada. Orquesta la validación del token del
    /// proveedor de identidad indicado (vía ITokenValidator), la resolución del
    /// usuario y la emisión del JWT propio. Es agnóstico del proveedor concreto.
    /// </summary>
    public interface IAuthService : IScopedService
    {
        /// <summary>
        /// Autentica a un usuario a partir del id_token emitido por el proveedor indicado.
        /// </summary>
        /// <param name="idToken">El id_token del proveedor de identidad.</param>
        /// <param name="provider">Clave del proveedor que emitió el token (ej: "google", "azure").</param>
        /// <returns>Resultado de la autenticación con información del usuario y token</returns>
        Task<AuthenticationResult> AuthenticateWithTokenAsync(string idToken, string provider);
    }
}

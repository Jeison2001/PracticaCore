using Domain.Common.Auth;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Auth
{
    /// <summary>
    /// Servicio para validar tokens de autenticación de terceros.
    /// Abstracción que permite cambiar de proveedor (Google, Azure AD, Auth0, etc.) sin impactar el dominio.
    /// </summary>
    public interface ITokenValidator : IScopedService
    {
        /// <summary>
        /// Valida un token de identificación y retorna el payload extraído.
        /// </summary>
        /// <param name="idToken">Token de identificación a validar</param>
        /// <returns>Payload con la información del usuario autenticado</returns>
        /// <exception cref="UnauthorizedAccessException">Si el token es inválido o ha expirado</exception>
        Task<TokenPayload> ValidateAsync(string idToken);
    }
}

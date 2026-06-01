using Domain.Common.Auth;

namespace Domain.Interfaces.Services.Auth
{
    /// <summary>
    /// Servicio para validar tokens de autenticación de terceros.
    /// Abstracción que permite habilitar varios proveedores (Google, Azure AD, etc.)
    /// de forma concurrente. Cada implementación declara qué proveedor maneja vía Provider.
    /// La selección de la implementación se hace por configuración (Authentication:Providers).
    /// </summary>
    public interface ITokenValidator
    {
        /// <summary>
        /// Clave del proveedor que maneja este validador (ej: "google", "azure").
        /// Debe coincidir con el valor enviado por el cliente al autenticarse.
        /// </summary>
        string Provider { get; }

        /// <summary>
        /// Valida un token de identificación y retorna el payload extraído.
        /// </summary>
        /// <param name="idToken">Token de identificación a validar</param>
        /// <returns>Payload con la información del usuario autenticado</returns>
        /// <exception cref="UnauthorizedAccessException">Si el token es inválido o ha expirado</exception>
        Task<TokenPayload> ValidateAsync(string idToken);
    }
}

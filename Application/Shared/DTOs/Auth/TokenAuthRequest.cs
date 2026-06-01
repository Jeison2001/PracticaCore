namespace Application.Shared.DTOs.Auth
{
    /// <summary>
    /// Petición de login federado. Contiene el id_token emitido por el proveedor
    /// de identidad y la clave del proveedor con el que debe validarse.
    /// </summary>
    public record TokenAuthRequest
    {
        public string IdToken { get; set; } = string.Empty;

        /// <summary>
        /// Proveedor de identidad que emitió el token (ej: "google", "azure").
        /// </summary>
        public string Provider { get; set; } = string.Empty;
    }
}

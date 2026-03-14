namespace Domain.Common.Auth
{
    /// <summary>
    /// Payload de un token de autenticación validado.
    /// Esta abstracción permite desacoplar el dominio de librerías específicas de proveedores.
    /// </summary>
    public record TokenPayload
    {
        public string Email { get; init; } = string.Empty;
        public string GivenName { get; init; } = string.Empty;
        public string FamilyName { get; init; } = string.Empty;
    }
}

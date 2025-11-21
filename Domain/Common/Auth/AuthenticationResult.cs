namespace Domain.Common.Auth
{
    /// <summary>
    /// Resultado de un proceso de autenticación en el Dominio.
    /// Contiene la información esencial del usuario autenticado y sus permisos.
    /// </summary>
    public record AuthenticationResult
    {
        public string Token { get; init; } = string.Empty;
        public UserInfoResult User { get; init; } = new();
        public List<RoleInfoResult> Roles { get; init; } = new();
        public Dictionary<string, object> Permissions { get; init; } = new();
    }
}

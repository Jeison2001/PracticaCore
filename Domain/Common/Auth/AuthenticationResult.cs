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

    public record UserInfoResult
    {
        public int Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Identification { get; init; } = string.Empty;
        public int IdIdentificationType { get; init; }
    }

    public record RoleInfoResult
    {
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
    }
}

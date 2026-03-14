namespace Domain.Common.Auth
{
    public record RoleInfoResult
    {
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
    }
}

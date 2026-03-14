namespace Domain.Common.Auth
{
    public record PermissionInfo
    {
        public string Code { get; init; } = string.Empty;
        public string? ParentCode { get; init; }
    }
}
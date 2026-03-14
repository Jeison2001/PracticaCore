namespace Domain.Common.Auth
{
    public record UserLoginData
    {
        public List<RoleInfoResult> Roles { get; init; } = new();
        public List<PermissionInfo> Permissions { get; init; } = new();
    }
}
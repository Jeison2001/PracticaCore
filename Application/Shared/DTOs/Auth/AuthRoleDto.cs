namespace Application.Shared.DTOs.Auth
{
    public record AuthRoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}

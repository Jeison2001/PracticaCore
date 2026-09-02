namespace Application.Shared.DTOs.Roles
{
    public record RoleSimpleDto
    {
        public int Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
    }
}

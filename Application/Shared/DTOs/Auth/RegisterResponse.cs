namespace Application.Shared.DTOs.Auth
{
    public record RegisterResponse
    {
        public long Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}

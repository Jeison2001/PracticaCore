namespace Application.Shared.DTOs.Auth
{
    public record ManualAuthRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}

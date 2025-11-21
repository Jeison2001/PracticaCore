namespace Application.Shared.DTOs.Auth
{
    public record GoogleAuthRequest
    {
        public string IdToken { get; set; } = string.Empty;
    }
}

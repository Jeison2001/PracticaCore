namespace Domain.Common.Auth
{
    public record UserInfoResult
    {
        public int Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Identification { get; init; } = string.Empty;
        public int IdIdentificationType { get; init; }
    }
}

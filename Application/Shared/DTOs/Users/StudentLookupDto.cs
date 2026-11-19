namespace Application.Shared.DTOs.Users
{
    public record StudentLookupDto
    {
        public int IdIdentificationType { get; set; }
        public string Identification { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}

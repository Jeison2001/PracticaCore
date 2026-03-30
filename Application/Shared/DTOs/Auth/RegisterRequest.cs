namespace Application.Shared.DTOs.Auth
{
    public record RegisterRequest
    {
        public int IdIdentificationType { get; set; }
        public string Identification { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int RoleId { get; set; }
        public int IdAcademicProgram { get; set; }
    }
}

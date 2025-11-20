namespace Application.Shared.DTOs.Users
{
    public class UserDto : BaseDto<int>
    {
        public int IdIdentificationType { get; set; }
        public string Identification { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int IdAcademicProgram { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CurrentAcademicPeriod { get; set; }
        public double? CumulativeAverage { get; set; }
        public int? ApprovedCredits { get; set; }
        public int? TotalAcademicCredits { get; set; }
        public string? Observation { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}
namespace Application.Shared.DTOs.AcademicPractices
{
    public record AcademicPracticeStateDetailDto
    {
        public string StateCode { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTimeOffset? CompletionDate { get; set; }
    }
}


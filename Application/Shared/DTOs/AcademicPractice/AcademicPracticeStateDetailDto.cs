namespace Application.Shared.DTOs.AcademicPractice
{
    public class AcademicPracticeStateDetailDto
    {
        public string StateCode { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime? CompletionDate { get; set; }
    }
}

namespace Application.Shared.DTOs.AcademicPractices
{
    public record AcademicPracticePhaseDetailDto
    {
        public int PhaseNumber { get; set; }
        public string PhaseName { get; set; } = string.Empty;
        public string PhaseCode { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTimeOffset? CompletionDate { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public List<AcademicPracticeStateDetailDto> States { get; set; } = new();
    }
}


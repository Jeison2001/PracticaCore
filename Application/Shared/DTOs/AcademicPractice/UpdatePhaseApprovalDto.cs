namespace Application.Shared.DTOs.AcademicPractice
{
    public class UpdatePhaseApprovalDto
    {
        public string PhaseType { get; set; } = string.Empty; // "Aval", "Plan", "Development", "FinalReport", "Final"
        public DateTime? ApprovalDate { get; set; }
        public string? Observations { get; set; }
        public string? EvaluatorObservations { get; set; }
    }
}

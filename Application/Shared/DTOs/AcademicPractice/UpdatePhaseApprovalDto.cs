using Application.Shared.DTOs;

namespace Application.Shared.DTOs.AcademicPractice
{
    public class UpdatePhaseApprovalDto : BaseDto<int>
    {
        public int NewStateStageId { get; set; } // Nuevo estado de la fase
        public string PhaseType { get; set; } = string.Empty; // "Aval", "Plan", "Development", "FinalReport", "Final"
        public DateTime? ApprovalDate { get; set; }
        public string? Observations { get; set; }
        public string? EvaluatorObservations { get; set; }
    }
}

namespace Application.Shared.DTOs.AcademicPractices
{
    public class UpdatePhaseApprovalDto : BaseDto<int>
    {
        public int NewStateStageId { get; set; } // Nuevo estado de la fase
        public string? Observations { get; set; }
        public string? EvaluatorObservations { get; set; }
    }
}

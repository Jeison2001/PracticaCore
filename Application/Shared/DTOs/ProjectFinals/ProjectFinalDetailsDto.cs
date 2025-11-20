using Application.Shared.DTOs.StateStages;

namespace Application.Shared.DTOs.ProjectFinals
{
    public class ProjectFinalDetailsDto : BaseDto<int>
    {
        public int IdStateStage { get; set; }
        public DateTime? ReportApprovalDate { get; set; }
        public DateTime? FinalPhaseApprovalDate { get; set; }
        public string? Observations { get; set; }
        public StateStageDto? StateStage { get; set; }
        // Agrega aquí cualquier campo adicional que quieras exponer
    }
}

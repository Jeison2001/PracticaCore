using Application.Shared.DTOs.StateStages;

namespace Application.Shared.DTOs.ProjectFinals
{
    public record ProjectFinalDetailsDto : BaseDto<int>
    {
        public int IdStateStage { get; set; }
        public DateTimeOffset? ReportApprovalDate { get; set; }
        public DateTimeOffset? FinalPhaseApprovalDate { get; set; }
        public string? Observations { get; set; }
        public StateStageDto? StateStage { get; set; }
        // Agrega aquí cualquier campo adicional que quieras exponer
    }
}


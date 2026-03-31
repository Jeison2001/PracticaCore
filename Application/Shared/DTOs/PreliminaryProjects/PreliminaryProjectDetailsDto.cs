using Application.Shared.DTOs.StateStages;

namespace Application.Shared.DTOs.PreliminaryProjects
{
    public record PreliminaryProjectDetailsDto : BaseDto<int>
    {
        public int IdStateStage { get; set; }
        public DateTimeOffset? ApprovalDate { get; set; }
        public string? Observations { get; set; }
        public StateStageDto? StateStage { get; set; }
        // Agrega aquí cualquier campo adicional que quieras exponer
    }
}


using System;
using Application.Shared.DTOs.StateStage;

namespace Application.Shared.DTOs.ProjectFinal
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

using System;

namespace Application.Shared.DTOs.ProjectFinal
{    public class ProjectFinalDto : BaseDto<int>
    {
        public int IdStateStage { get; set; }
        public DateTime? ReportApprovalDate { get; set; }
        public DateTime? FinalPhaseApprovalDate { get; set; }
        public string? Observations { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}
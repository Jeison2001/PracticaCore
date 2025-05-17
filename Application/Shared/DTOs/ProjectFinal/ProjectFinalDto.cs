using System;
using Application.Shared.DTOs.StateProjectFinal;

namespace Application.Shared.DTOs.ProjectFinal
{
    public class ProjectFinalDto : BaseDto<int>
    {
        public int IdStateProjectFinal { get; set; }
        public DateTime? ReportApprovalDate { get; set; }
        public DateTime? FinalPhaseApprovalDate { get; set; }
        public string? Observations { get; set; }
        public StateProjectFinalDto? StateProjectFinal { get; set; }
    }
}
using System;

namespace Domain.Entities
{
    public class ProjectFinal : BaseEntity<int>
    {
        public int IdStateProjectFinal { get; set; }
        public DateTime? ReportApprovalDate { get; set; }
        public DateTime? FinalPhaseApprovalDate { get; set; }
        public string? Observations { get; set; }
        public new int? IdUserCreatedAt { get; set; }

        public StateProjectFinal? StateProjectFinal { get; set; }
    }
}
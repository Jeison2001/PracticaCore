namespace Domain.Entities
{    public class ProjectFinal : BaseEntity<int>
    {
        public int IdStateStage { get; set; }
        public DateTimeOffset? ReportApprovalDate { get; set; }
        public DateTimeOffset? FinalPhaseApprovalDate { get; set; }
        public string? Observations { get; set; }

        public virtual StateStage? StateStage { get; set; }
    }
}
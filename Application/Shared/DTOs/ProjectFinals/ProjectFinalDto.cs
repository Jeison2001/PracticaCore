namespace Application.Shared.DTOs.ProjectFinals
{    public record ProjectFinalDto : BaseDto<int>
    {
        public int IdStateStage { get; set; }
        public DateTime? ReportApprovalDate { get; set; }
        public DateTime? FinalPhaseApprovalDate { get; set; }
        public string? Observations { get; set; }
    }
}

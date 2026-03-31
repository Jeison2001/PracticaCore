namespace Application.Shared.DTOs.ProjectFinals
{    public record ProjectFinalDto : BaseDto<int>
    {
        public int IdStateStage { get; set; }
        public DateTimeOffset? ReportApprovalDate { get; set; }
        public DateTimeOffset? FinalPhaseApprovalDate { get; set; }
        public string? Observations { get; set; }
    }
}


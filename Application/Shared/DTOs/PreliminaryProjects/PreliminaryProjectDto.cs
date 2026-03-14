namespace Application.Shared.DTOs.PreliminaryProjects
{    public record PreliminaryProjectDto : BaseDto<int>
    {
        public int IdStateStage { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? Observations { get; set; }
    }
}

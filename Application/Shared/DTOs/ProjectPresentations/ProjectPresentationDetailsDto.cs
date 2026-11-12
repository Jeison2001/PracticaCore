using Application.Shared.DTOs.StateStages;

namespace Application.Shared.DTOs.ProjectPresentations
{
    public record ProjectPresentationDetailsDto : BaseDto<int>
    {
        public int IdInscriptionModality { get; set; }
        public int IdStateStage { get; set; }
        public DateTimeOffset? ScheduledDateTime { get; set; }
        public DateTimeOffset? ActualDateTime { get; set; }
        public string? LocationOrLink { get; set; }
        public string? PresentationModality { get; set; }
        public int AttemptNumber { get; set; } = 1;
        public string? Observations { get; set; }
        public StateStageDto? StateStage { get; set; }
    }
}

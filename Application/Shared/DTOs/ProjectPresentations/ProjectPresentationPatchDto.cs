namespace Application.Shared.DTOs.ProjectPresentations
{
    public record ProjectPresentationPatchDto
    {
        public int? IdStateStage { get; set; }
        public DateTimeOffset? ScheduledDateTime { get; set; }
        public DateTimeOffset? ActualDateTime { get; set; }
        public string? LocationOrLink { get; set; }
        public string? PresentationModality { get; set; }
        public string? Observations { get; set; }
    }
}

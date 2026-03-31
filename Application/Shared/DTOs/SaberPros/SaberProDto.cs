namespace Application.Shared.DTOs.SaberPros
{
    public record SaberProDto : BaseDto<int>
    {
        public int IdStateStage { get; set; }
        public DateTimeOffset? ExamDate { get; set; }
        public string? ResultQuintile { get; set; }
        public decimal? ResultScore { get; set; }
        public string? Observations { get; set; }
    }
}


namespace Application.Shared.DTOs.CoTerminals
{
    public record CoTerminalDto : BaseDto<int>
    {
        public int IdStateStage { get; set; }
        public string? PostgraduateProgramName { get; set; }
        public string? UniversityName { get; set; }
        public decimal? FirstSemesterAverage { get; set; }
        public string? Observations { get; set; }
    }
}

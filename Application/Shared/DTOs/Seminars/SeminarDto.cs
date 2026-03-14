namespace Application.Shared.DTOs.Seminars
{
    public record SeminarDto : BaseDto<int>
    {
        public int IdStateStage { get; set; }
        public string? SeminarName { get; set; }
        public decimal? AttendancePercentage { get; set; }
        public decimal? FinalGrade { get; set; }
        public string? Observations { get; set; }
    }
}

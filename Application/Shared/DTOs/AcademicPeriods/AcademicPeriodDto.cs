namespace Application.Shared.DTOs.AcademicPeriods
{
    public record AcademicPeriodDto : BaseDto<int>
    {
        public string Code { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}

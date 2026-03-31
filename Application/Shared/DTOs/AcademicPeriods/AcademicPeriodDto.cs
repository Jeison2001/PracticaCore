namespace Application.Shared.DTOs.AcademicPeriods
{
    public record AcademicPeriodDto : BaseDto<int>
    {
        public string Code { get; set; } = string.Empty;
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
    }
}


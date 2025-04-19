using System;

namespace Application.Shared.DTOs.AcademicPeriod
{
    public class AcademicPeriodDto : BaseDto<long>
    {
        public string Code { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}
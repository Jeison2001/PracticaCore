using System;

namespace Domain.Entities
{
    public class AcademicPeriod : BaseEntity<int>
    {
        public string Code { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}
namespace Domain.Entities
{
    public class AcademicPeriod : BaseEntity<int>
    {
        public string Code { get; set; } = string.Empty;
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
    }
}
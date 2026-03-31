namespace Domain.Common.AcademicPractice
{
    public class StateDetail
    {
        public string StateCode { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTimeOffset? CompletionDate { get; set; }
    }
}

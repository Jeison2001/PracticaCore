namespace Domain.Common.AcademicPractice
{
    public class PhaseDetail
    {
        public int PhaseNumber { get; set; }
        public string PhaseName { get; set; } = string.Empty;
        public string PhaseCode { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public List<StateDetail> States { get; set; } = new();
    }
}

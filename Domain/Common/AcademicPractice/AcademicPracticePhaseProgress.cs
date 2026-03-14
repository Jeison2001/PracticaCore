namespace Domain.Common.AcademicPractice
{
    public class AcademicPracticePhaseProgress
    {
        public string CurrentPhase { get; set; } = string.Empty;
        public string CurrentState { get; set; } = string.Empty;
        public bool Phase1Completed { get; set; }
        public bool Phase2Completed { get; set; }
        public bool Phase3Completed { get; set; }
        public DateTime? Phase1CompletionDate { get; set; }
        public DateTime? Phase2CompletionDate { get; set; }
        public DateTime? Phase3CompletionDate { get; set; }
        public List<PhaseDetail> PhaseDetails { get; set; } = new();
    }
}

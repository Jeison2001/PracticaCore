namespace Domain.Common.Notifications
{
    public class SlaAlertInscriptionCandidate
    {
        public int InscriptionId { get; set; }
        public string ModalityCode { get; set; } = string.Empty;
        public string ModalityName { get; set; } = string.Empty;
        public int? MaximumTermPeriods { get; set; }
        public string StateInscriptionCode { get; set; } = string.Empty;
        public string StateInscriptionName { get; set; } = string.Empty;
        public string AcademicPeriodCode { get; set; } = string.Empty;
        public DateTimeOffset? PeriodStartDate { get; set; }
        public int PeriodsElapsed { get; set; }
        public string StageCode { get; set; } = string.Empty;
        public string StageName { get; set; } = string.Empty;
        public string EffectiveStateStageCode { get; set; } = string.Empty;
        public string EffectiveStateStageName { get; set; } = string.Empty;
        public DateTimeOffset LastActivityAt { get; set; }
        public string WorkTitle { get; set; } = string.Empty;
        public string LatestObservations { get; set; } = string.Empty;
        public string StudentNames { get; set; } = string.Empty;
        public string StudentEmails { get; set; } = string.Empty;
        public int? FacultyId { get; set; }
        public DateTimeOffset? ScheduledDateTime { get; set; }
        public string PresentationModality { get; set; } = string.Empty;
        public string LocationOrLink { get; set; } = string.Empty;
        public int AttemptNumber { get; set; } = 1;
        public string JuryNames { get; set; } = string.Empty;
    }
}

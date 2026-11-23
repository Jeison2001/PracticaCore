namespace Domain.Common.Dashboard
{
    public record StaleInscriptionAlert(
        int InscriptionId,
        string ModalityCode,
        string ModalityName,
        string Students,
        string CurrentPhase,
        string PhaseState,
        string LifecycleCode,
        string LifecycleStatus,
        int DaysSinceActivity,
        string LastActivityAt,
        bool SlaBreachAcuerdo015,
        string ResponsibleParty);

    public record TermRiskAlert(
        int InscriptionId,
        string ModalityCode,
        string ModalityName,
        string Students,
        string StartPeriod,
        int PeriodsElapsed,
        int MaxTermPeriods,
        string RiskLevel,
        string CurrentPhase,
        string PhaseState);

    public record UpcomingDefenseAlert(
        int InscriptionId,
        string Title,
        string Students,
        string ScheduledDateTime,
        string PresentationModality,
        string LocationOrLink,
        int AttemptNumber,
        string Juries);

    public record ReworkModalityMetric(
        string ModalityCode,
        string ModalityName,
        int TotalInscriptions,
        int ObservationEvaluations,
        double AvgObservationsPerInscription,
        int ReplacedDocumentsCount);

    public class DashboardOperationalAlerts
    {
        public int StaleThresholdDays { get; set; } = 30;
        public int EvaluatorOverdueBusinessDays { get; set; } = 25;
        public int TotalStaleCount { get; set; }
        public int TotalEvaluationSlaBreachCount { get; set; }
        public int TotalTermRiskCount { get; set; }
        public List<StaleInscriptionAlert> StaleItems { get; set; } = new();
        public List<TermRiskAlert> TermRiskItems { get; set; } = new();
        public List<UpcomingDefenseAlert> UpcomingDefenses { get; set; } = new();
        public List<ReworkModalityMetric> ReworkByModality { get; set; } = new();
    }
}

namespace Domain.Entities
{
    public class SlaAlertConfiguration : BaseEntity<int>
    {
        public bool IsEnabled { get; set; } = true;
        public int ScheduleHourColombia { get; set; } = 7;
        public int ScheduleMinuteColombia { get; set; } = 30;
        public bool WeekdaysOnly { get; set; } = false;
        public int EvaluatorWarningBusinessDays { get; set; } = 20;
        public int EvaluatorOverdueBusinessDays { get; set; } = 25;
        public int CommitteeOverdueBusinessDays { get; set; } = 10;
        public int StudentObservationsBusinessDays { get; set; } = 15;
        public int DefenseReminderHours { get; set; } = 48;
        public bool WarnOnLastAllowedPeriod { get; set; } = true;
        public DateTimeOffset? LastRunAt { get; set; }
        public int? LastRunEvaluatedCount { get; set; }
        public int? LastRunTriggeredCount { get; set; }
        public int? LastRunSkippedCount { get; set; }
        public int? LastRunFailedCount { get; set; }
    }
}

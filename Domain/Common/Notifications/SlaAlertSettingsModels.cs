namespace Domain.Common.Notifications
{
    public class SlaAlertSettingsOverviewDto
    {
        public SlaAlertGlobalSettingsDto Global { get; set; } = new();
        public List<SlaAlertCcOptionDto> AvailableCcOptions { get; set; } = new();
        public List<SlaAlertRuleSettingDto> Rules { get; set; } = new();
        public List<ModalityTermLimitDto> ModalityTermLimits { get; set; } = new();
    }

    public class SlaAlertGlobalSettingsDto
    {
        public bool IsEnabled { get; set; } = true;
        public int ScheduleHourColombia { get; set; } = 7;
        public int ScheduleMinuteColombia { get; set; } = 30;
        public bool WeekdaysOnly { get; set; }
        public string CronExpression { get; set; } = "30 7 * * *";
        public int EvaluatorWarningBusinessDays { get; set; } = 20;
        public int EvaluatorOverdueBusinessDays { get; set; } = 25;
        public int CommitteeOverdueBusinessDays { get; set; } = 10;
        public int StudentObservationsBusinessDays { get; set; } = 15;
        public int DefenseReminderHours { get; set; } = 48;
        public bool WarnOnLastAllowedPeriod { get; set; } = true;
        public DateTimeOffset? LastRunAtUtc { get; set; }
        public string? LastRunAtColombia { get; set; }
        public int? LastRunEvaluatedCount { get; set; }
        public int? LastRunTriggeredCount { get; set; }
        public int? LastRunSkippedCount { get; set; }
        public int? LastRunFailedCount { get; set; }
        public DateTimeOffset? UpdatedAtUtc { get; set; }
        public string? UpdatedAtColombia { get; set; }
    }

    public class SlaAlertCcOptionDto
    {
        public string Key { get; set; } = string.Empty;
        public string RuleType { get; set; } = string.Empty;
        public string RuleValue { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class SlaAlertRuleSettingDto
    {
        public string EventName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string NormativeReference { get; set; } = string.Empty;
        public string ThresholdLabel { get; set; } = string.Empty;
        public string PrimaryRecipientsLabel { get; set; } = string.Empty;
        public string PrimaryResolutionMode { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public List<string> SelectedCcKeys { get; set; } = new();
        public string SampleSubject { get; set; } = string.Empty;
        public string SampleHtml { get; set; } = string.Empty;
    }

    public class ModalityTermLimitDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? MaximumTermPeriods { get; set; }
        public bool AllowsExtension { get; set; }
    }

    public class SlaAlertGlobalInputDto
    {
        public bool IsEnabled { get; set; } = true;
        public int ScheduleHourColombia { get; set; }
        public int ScheduleMinuteColombia { get; set; }
        public bool WeekdaysOnly { get; set; }
        public int EvaluatorWarningBusinessDays { get; set; }
        public int EvaluatorOverdueBusinessDays { get; set; }
        public int CommitteeOverdueBusinessDays { get; set; }
        public int StudentObservationsBusinessDays { get; set; }
        public int DefenseReminderHours { get; set; }
        public bool WarnOnLastAllowedPeriod { get; set; } = true;
    }

    public class UpdateSlaAlertSettingsRequest
    {
        public bool IsEnabled { get; set; } = true;
        public int ScheduleHourColombia { get; set; }
        public int ScheduleMinuteColombia { get; set; }
        public bool WeekdaysOnly { get; set; }
        public int EvaluatorWarningBusinessDays { get; set; }
        public int EvaluatorOverdueBusinessDays { get; set; }
        public int CommitteeOverdueBusinessDays { get; set; }
        public int StudentObservationsBusinessDays { get; set; }
        public int DefenseReminderHours { get; set; }
        public bool WarnOnLastAllowedPeriod { get; set; } = true;
        public SlaAlertGlobalInputDto? Global { get; set; }
        public List<UpdateSlaAlertRuleRequest>? Rules { get; set; }
    }

    public class UpdateSlaAlertRuleRequest
    {
        public string EventName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public List<string>? SelectedCcKeys { get; set; }
    }

    public class SlaAlertDispatchLogsPageDto
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<SlaAlertDispatchLogItemDto> Items { get; set; } = new();
    }

    public class SlaAlertDispatchLogItemDto
    {
        public int Id { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public string CreatedAtColombia { get; set; } = string.Empty;
        public int IdInscriptionModality { get; set; }
        public string ModalityName { get; set; } = string.Empty;
        public string StudentsSummary { get; set; } = string.Empty;
        public string AlertCode { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string StageStateSnapshot { get; set; } = string.Empty;
        public int BusinessDaysElapsed { get; set; }
        public string? RecipientsSummary { get; set; }
    }

    public record SlaAlertCcTarget(string RuleType, string RuleValue);

    public record SlaAlertDefaultToRule(string RuleType, string RuleValue, int Priority);

    public record SlaAlertRulePersistenceInstruction(
        string EventName,
        bool IsActive,
        IReadOnlyList<SlaAlertCcTarget> SelectedCcTargets,
        string DefaultSubjectTemplate,
        string DefaultBodyTemplate,
        IReadOnlyList<SlaAlertDefaultToRule> DefaultToRules);

    public record SlaAlertSettingsSnapshot(
        Domain.Entities.SlaAlertConfiguration? Configuration,
        List<Domain.Entities.EmailNotificationConfig> Configs,
        List<Domain.Entities.EmailRecipientRule> ActiveCcRules,
        List<ModalityTermLimitDto> ModalityTermLimits);
}

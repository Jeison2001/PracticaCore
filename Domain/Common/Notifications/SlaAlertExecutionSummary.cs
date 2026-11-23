namespace Domain.Common.Notifications
{
    public record SlaAlertExecutionSummary(
        int EvaluatedInscriptions,
        int AlertsTriggered,
        int AlertsSkippedAlreadySent,
        int AlertsFailed,
        List<string> DispatchedKeys);
}

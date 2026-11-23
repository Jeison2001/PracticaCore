using Domain.Common.Notifications;
using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Repositories
{
    public interface ISlaAlertRepository : IScopedService
    {
        Task<SlaAlertConfiguration?> GetConfigurationAsync(
            CancellationToken cancellationToken = default);

        Task SaveConfigurationRunMetricsAsync(
            DateTimeOffset runAtUtc,
            SlaAlertExecutionSummary summary,
            CancellationToken cancellationToken = default);

        Task<List<SlaAlertInscriptionCandidate>> GetActiveCandidatesAsync(
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default);

        Task<HashSet<string>> GetExistingDeduplicationKeysAsync(
            IEnumerable<string> candidateKeys,
            CancellationToken cancellationToken = default);

        Task<bool> TryClaimDispatchAsync(
            EmailAlertDispatchLog entry,
            CancellationToken cancellationToken = default);

        Task ConfirmDispatchClaimAsync(
            int logId,
            string recipientsSummary,
            CancellationToken cancellationToken = default);

        Task ReleaseDispatchClaimAsync(
            int logId,
            CancellationToken cancellationToken = default);

        Task<SlaAlertSettingsSnapshot> GetSettingsSnapshotAsync(
            IReadOnlyCollection<string> slaEventNames,
            CancellationToken cancellationToken = default);

        Task SaveSettingsAsync(
            SlaAlertConfiguration updatedGlobal,
            IReadOnlyList<SlaAlertRulePersistenceInstruction> ruleInstructions,
            int? currentUserId,
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default);

        Task<SlaAlertDispatchLogsPageDto> GetDispatchLogsAsync(
            int page,
            int pageSize,
            string? alertCode,
            string? search,
            CancellationToken cancellationToken = default);
    }
}

using Domain.Common.Notifications;
using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Repositories
{
    public interface ISlaAlertRepository : IScopedService
    {
        Task<List<SlaAlertInscriptionCandidate>> GetActiveCandidatesAsync(
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default);

        Task<HashSet<string>> GetExistingDeduplicationKeysAsync(
            IEnumerable<string> candidateKeys,
            CancellationToken cancellationToken = default);

        Task<bool> TryRecordDispatchAsync(
            EmailAlertDispatchLog logEntry,
            CancellationToken cancellationToken = default);
    }
}

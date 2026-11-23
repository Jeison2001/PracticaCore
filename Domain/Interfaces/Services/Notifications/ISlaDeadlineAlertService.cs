using Domain.Common.Notifications;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications
{
    public interface ISlaDeadlineAlertService : IScopedService
    {
        Task<SlaAlertExecutionSummary> ProcessDueAlertsAsync(CancellationToken cancellationToken = default);
    }
}

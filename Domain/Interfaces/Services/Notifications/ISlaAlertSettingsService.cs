using Domain.Common.Notifications;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications
{
    public interface ISlaAlertSettingsService : IScopedService
    {
        Task<SlaAlertSettingsOverviewDto> GetOverviewAsync(
            CancellationToken cancellationToken = default);

        Task<(SlaAlertSettingsOverviewDto? Overview, List<string> ValidationErrors)> UpdateSettingsAsync(
            UpdateSlaAlertSettingsRequest? request,
            int? currentUserId,
            CancellationToken cancellationToken = default);

        Task<SlaAlertDispatchLogsPageDto> GetLogsAsync(
            int page,
            int pageSize,
            string? alertCode,
            string? search,
            CancellationToken cancellationToken = default);
    }
}

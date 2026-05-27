using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications.Builders
{
    public interface ISeminarEventDataBuilder : IScopedService
    {
        Task<Dictionary<string, object>> BuildEventDataAsync(int entityId, string eventType);
    }
}

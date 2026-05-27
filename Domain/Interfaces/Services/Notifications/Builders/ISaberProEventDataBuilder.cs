using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications.Builders
{
    public interface ISaberProEventDataBuilder : IScopedService
    {
        Task<Dictionary<string, object>> BuildEventDataAsync(int entityId, string eventType);
    }
}

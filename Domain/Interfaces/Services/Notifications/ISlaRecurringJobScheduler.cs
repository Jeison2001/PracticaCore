using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications
{
    public interface ISlaRecurringJobScheduler : IScopedService
    {
        void SyncRecurringJob(bool isEnabled, int hourColombia, int minuteColombia, bool weekdaysOnly);
    }
}

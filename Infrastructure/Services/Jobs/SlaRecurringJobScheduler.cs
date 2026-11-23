using Domain.Common.Dashboard;
using Domain.Common.Notifications;
using Domain.Interfaces.Services.Notifications;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Jobs
{
    public class SlaRecurringJobScheduler : ISlaRecurringJobScheduler
    {
        public const string SlaDeadlineAlertsJobId = "sigepro-sla-deadline-alerts";

        private readonly ILogger<SlaRecurringJobScheduler> _logger;

        public SlaRecurringJobScheduler(ILogger<SlaRecurringJobScheduler> logger)
        {
            _logger = logger;
        }

        public void SyncRecurringJob(bool isEnabled, int hourColombia, int minuteColombia, bool weekdaysOnly)
        {
            try
            {
                if (!isEnabled)
                {
                    RecurringJob.RemoveIfExists(SlaDeadlineAlertsJobId);
                    return;
                }

                var cron = SlaCronHelper.BuildCron(hourColombia, minuteColombia, weekdaysOnly);
                RecurringJob.AddOrUpdate<ISlaDeadlineAlertService>(
                    SlaDeadlineAlertsJobId,
                    service => service.ProcessDueAlertsAsync(CancellationToken.None),
                    cron,
                    new RecurringJobOptions
                    {
                        TimeZone = DashboardTimeHelper.ColombiaTimeZone
                    });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogDebug(ex, "Hangfire JobStorage no inicializado; se omite sincronización del job recurrente SLA.");
            }
        }
    }
}

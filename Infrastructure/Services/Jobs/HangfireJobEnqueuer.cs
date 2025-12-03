using System.Linq.Expressions;
using Domain.Interfaces.Services.Jobs;
using Hangfire;

namespace Infrastructure.Services.Jobs
{
    public class HangfireJobEnqueuer : IJobEnqueuer
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public HangfireJobEnqueuer(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        public string Enqueue<T>(Expression<Action<T>> methodCall)
        {
            return _backgroundJobClient.Enqueue<T>(methodCall);
        }
    }
}

using System.Linq.Expressions;
using Domain.Interfaces.Services.Jobs;

namespace Tests.Integration.Utilities
{
    public class TestJobEnqueuer : IJobEnqueuer
    {
        private readonly List<EnqueuedJobInfo> _enqueuedJobs;

        public TestJobEnqueuer(List<EnqueuedJobInfo> enqueuedJobs)
        {
            _enqueuedJobs = enqueuedJobs;
        }

        public string Enqueue<T>(Expression<Action<T>> methodCall)
        {
            var jobInfo = new EnqueuedJobInfo
            {
                JobType = typeof(T).Name,
                MethodName = ((MethodCallExpression)methodCall.Body).Method.Name,
                EnqueuedAt = DateTime.UtcNow
            };

            lock (_enqueuedJobs)
            {
                _enqueuedJobs.Add(jobInfo);
            }

            return $"test-job-id-{_enqueuedJobs.Count}";
        }
    }

    public class EnqueuedJobInfo
    {
        public string JobType { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public DateTime EnqueuedAt { get; set; }
    }
}

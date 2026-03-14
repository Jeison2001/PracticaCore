using System.Linq.Expressions;
using Domain.Interfaces.Services.Jobs;

namespace Tests.Integration.Utilities
{
    public class TestJobEnqueuer : IJobEnqueuer
    {
        public string Enqueue<T>(Expression<Action<T>> methodCall)
        {
            // In tests, we don't want to run background jobs or we want to run them immediately.
            // For now, we just return a dummy Job ID and do nothing, 
            // effectively mocking the "Enqueue" part.
            // If we wanted to test the side effects, we would need to execute the expression here.
            return "test-job-id";
        }
    }
}

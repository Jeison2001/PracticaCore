using System.Linq.Expressions;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Jobs
{
    public interface IJobEnqueuer : ITransientService
    {
        string Enqueue<T>(Expression<Action<T>> methodCall);
    }
}

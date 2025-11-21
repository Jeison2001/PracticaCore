using Domain.Entities;

namespace Domain.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        IRepository<T, TId> GetRepository<T, TId>() where T : BaseEntity<TId> where TId : struct;
        Task<int> CommitAsync(CancellationToken ct = default);
    }
}

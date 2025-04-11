using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IRepository<T, TId> where T : BaseEntity<TId> where TId : struct
    {
        Task<T?> GetByIdAsync(TId id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task<T?> GetFirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);
    }
}

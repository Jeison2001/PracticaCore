using System.Linq.Expressions;
using Domain.Common;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IRepository<T, TId> where T : BaseEntity<TId> where TId : struct
    {
        Task<T?> GetByIdAsync(TId id);
        Task UpdatePartialAsync(T entity, Expression<Func<T, object>>[] updatedProperties);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);
        Task<IEnumerable<T>> GetAllAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int? pageNumber = null,
            int? pageSize = null);
        Task<PaginatedResult<T>> GetAllWithPaginationAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int pageNumber = 1,
            int pageSize = 10);
    }
}

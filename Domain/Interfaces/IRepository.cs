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
    }
}

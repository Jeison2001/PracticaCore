namespace Domain.Interfaces
{
    public interface IReadRepository<T> where T : class
    {
        Task<T> GetByIdAsync(int id);
    }
}
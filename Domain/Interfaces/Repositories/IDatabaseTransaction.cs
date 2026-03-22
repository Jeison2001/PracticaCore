namespace Domain.Interfaces.Repositories
{
    public interface IDatabaseTransaction : IDisposable, IAsyncDisposable
    {
        Task CommitAsync(CancellationToken ct = default);
        Task RollbackAsync(CancellationToken ct = default);
    }
}

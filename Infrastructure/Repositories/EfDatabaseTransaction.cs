using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Repositories
{
    public class EfDatabaseTransaction : IDatabaseTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public EfDatabaseTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync(CancellationToken ct = default)
        {
            return _transaction.CommitAsync(ct);
        }

        public Task RollbackAsync(CancellationToken ct = default)
        {
            return _transaction.RollbackAsync(ct);
        }

        public void Dispose()
        {
            _transaction.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            return _transaction.DisposeAsync();
        }
    }
}

using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Repositories
{
    public class EfDatabaseTransaction : IDatabaseTransaction
    {
        private readonly IDbContextTransaction? _transaction;
        private readonly Func<CancellationToken, Task>? _onCommittedAsync;
        private readonly Action? _onAborted;
        private bool _completed;

        public EfDatabaseTransaction(
            IDbContextTransaction? transaction,
            Func<CancellationToken, Task>? onCommittedAsync = null,
            Action? onAborted = null)
        {
            _transaction = transaction;
            _onCommittedAsync = onCommittedAsync;
            _onAborted = onAborted;
        }

        public async Task CommitAsync(CancellationToken ct = default)
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync(ct);
            }

            _completed = true;
            if (_onCommittedAsync != null)
            {
                await _onCommittedAsync(ct);
            }
        }

        public async Task RollbackAsync(CancellationToken ct = default)
        {
            try
            {
                if (_transaction != null)
                {
                    await _transaction.RollbackAsync(ct);
                }
            }
            finally
            {
                if (!_completed)
                {
                    _completed = true;
                    _onAborted?.Invoke();
                }
            }
        }

        public void Dispose()
        {
            try
            {
                _transaction?.Dispose();
            }
            finally
            {
                if (!_completed)
                {
                    _completed = true;
                    _onAborted?.Invoke();
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                }
            }
            finally
            {
                if (!_completed)
                {
                    _completed = true;
                    _onAborted?.Invoke();
                }
            }
        }
    }
}

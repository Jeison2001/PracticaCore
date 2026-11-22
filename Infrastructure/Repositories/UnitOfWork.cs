using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Domain.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly IMediator _mediator;
        private readonly Microsoft.Extensions.Logging.ILogger<UnitOfWork> _logger;
        private readonly Dictionary<Type, object> _repositories = [];
        private readonly List<Func<CancellationToken, Task>> _postCommitActions = [];
        private IDatabaseTransaction? _activeExplicitTransaction;
        private bool _isCommitting;

        private bool HasActiveExplicitTransaction =>
            _activeExplicitTransaction != null ||
            (_context.Database.IsRelational() && _context.Database.CurrentTransaction != null);

        public UnitOfWork(AppDbContext context, IMediator mediator, Microsoft.Extensions.Logging.ILogger<UnitOfWork> logger)
        {
            _context = context;
            _mediator = mediator;
            _logger = logger;
        }

        public IRepository<T, TId> GetRepository<T, TId>()
            where T : BaseEntity<TId>
            where TId : struct
        {
            if (_repositories.ContainsKey(typeof(T)))
                return (IRepository<T, TId>)_repositories[typeof(T)];

            var repo = new BaseRepository<T, TId>(_context);
            _repositories.Add(typeof(T), repo);
            return repo;
        }

        public async Task<int> CommitAsync(CancellationToken ct = default)
        {
            if (_isCommitting)
                return await _context.SaveChangesAsync(ct);

            _isCommitting = true;
            IDatabaseTransaction? ownedTransaction = null;
            int totalSaved;
            List<Func<CancellationToken, Task>> actionsToRun;

            try
            {
                var hasExternalTransaction = HasActiveExplicitTransaction;
                if (!hasExternalTransaction && _context.Database.IsRelational())
                {
                    var dbTransaction = await _context.Database.BeginTransactionAsync(ct);
                    ownedTransaction = new EfDatabaseTransaction(dbTransaction);
                }

                totalSaved = await _context.SaveChangesAsync(ct);

                const int maxRounds = 10;
                var round = 0;
                while (round < maxRounds)
                {
                    var domainEntities = _context.ChangeTracker
                        .Entries()
                        .Select(x => x.Entity)
                        .OfType<IHasDomainEvents>()
                        .Where(e => e.DomainEvents.Count > 0)
                        .ToList();

                    if (domainEntities.Count == 0)
                        break;

                    round++;
                    _logger.LogDebug("[DEBUG] DispatchDomainEventsAsync (round {Round}): Found {Count} entities with events", round, domainEntities.Count);

                    var domainEvents = new List<IDomainEvent>();
                    foreach (var entity in domainEntities)
                    {
                        domainEvents.AddRange(entity.DomainEvents);
                        entity.ClearDomainEvents();
                    }

                    _logger.LogDebug("[DEBUG] Publishing {Count} domain events", domainEvents.Count);
                    foreach (var domainEvent in domainEvents)
                    {
                        await _mediator.Publish((object)domainEvent, ct);
                    }

                    if (_context.ChangeTracker.HasChanges())
                    {
                        totalSaved += await _context.SaveChangesAsync(ct);
                    }
                }

                if (round == maxRounds && _context.ChangeTracker.Entries().Select(x => x.Entity).OfType<IHasDomainEvents>().Any(e => e.DomainEvents.Count > 0))
                {
                    _logger.LogWarning("UnitOfWork.CommitAsync alcanzó el límite de {MaxRounds} rondas de despacho de DomainEvents.", maxRounds);
                }

                if (ownedTransaction != null)
                {
                    await ownedTransaction.CommitAsync(ct);
                }

                if (hasExternalTransaction)
                {
                    // Difiere _postCommitActions hasta que transaction.CommitAsync() real confirme en BD.
                    actionsToRun = [];
                }
                else
                {
                    actionsToRun = [.. _postCommitActions];
                    _postCommitActions.Clear();
                }
            }
            catch
            {
                _postCommitActions.Clear();
                if (ownedTransaction != null)
                {
                    await ownedTransaction.RollbackAsync(CancellationToken.None);
                }
                throw;
            }
            finally
            {
                if (ownedTransaction != null)
                {
                    await ownedTransaction.DisposeAsync();
                }
                _isCommitting = false;
            }

            foreach (var action in actionsToRun)
            {
                await action(ct);
            }

            return totalSaved;
        }

        public async Task ExecuteAfterCommitAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (_isCommitting || HasActiveExplicitTransaction)
            {
                _postCommitActions.Add(action);
                return;
            }

            await action(ct);
        }

        public async Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken ct = default)
        {
            var dbTransaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(ct)
                : null;

            IDatabaseTransaction? wrapper = null;
            wrapper = new EfDatabaseTransaction(
                dbTransaction,
                onCommittedAsync: async commitCt =>
                {
                    if (ReferenceEquals(_activeExplicitTransaction, wrapper))
                    {
                        _activeExplicitTransaction = null;
                    }

                    var actionsToRun = _postCommitActions.ToList();
                    _postCommitActions.Clear();

                    foreach (var action in actionsToRun)
                    {
                        await action(commitCt);
                    }
                },
                onAborted: () =>
                {
                    if (ReferenceEquals(_activeExplicitTransaction, wrapper))
                    {
                        _activeExplicitTransaction = null;
                    }

                    _postCommitActions.Clear();
                });

            _activeExplicitTransaction = wrapper;
            return wrapper;
        }
    }
}

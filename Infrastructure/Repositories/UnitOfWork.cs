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
            await DispatchDomainEventsAsync();
            return await _context.SaveChangesAsync(ct);
        }

        private async Task DispatchDomainEventsAsync()
        {
            var domainEntities = _context.ChangeTracker
                .Entries()
                .Select(x => x.Entity)
                .Where(e => 
                {
                    var type = e.GetType();
                    if (type.FullName != null && type.FullName.StartsWith("Castle.Proxies"))
                    {
                        type = type.BaseType!;
                    }
                    var prop = type.GetProperty("DomainEvents");
                    if (prop == null) return false;
                    
                    var events = prop.GetValue(e) as IEnumerable<IDomainEvent>;
                    return events != null && events.Any();
                })
                .ToList();

            var domainEvents = new List<IDomainEvent>();

            foreach (var entity in domainEntities)
            {
                var type = entity.GetType();
                if (type.FullName != null && type.FullName.StartsWith("Castle.Proxies"))
                {
                    type = type.BaseType!;
                }

                var prop = type.GetProperty("DomainEvents");
                var method = type.GetMethod("ClearDomainEvents");

                if (prop != null && method != null)
                {
                    var events = (prop.GetValue(entity) as IEnumerable<IDomainEvent>)?.ToList();
                    if (events != null && events.Any())
                    {
                        domainEvents.AddRange(events);
                    }
                    method.Invoke(entity, null); // ClearDomainEvents()
                }
            }

            foreach (var domainEvent in domainEvents)
            {
                await _mediator.Publish((object)domainEvent);
            }
        }

        public async Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken ct = default)
        {
            var transaction = await _context.Database.BeginTransactionAsync(ct);
            return new EfDatabaseTransaction(transaction);
        }
    }
}

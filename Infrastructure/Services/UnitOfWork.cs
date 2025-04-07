using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;

namespace Infrastructure.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly Dictionary<Type, object> _repositories = [];

        public UnitOfWork(AppDbContext context) => _context = context;

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
            => await _context.SaveChangesAsync(ct);
    }
}

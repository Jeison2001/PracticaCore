using Domain.Interfaces;
using Domain.Interfaces.Cache;
using Infrastructure.Repositories.Cache;
using Microsoft.Extensions.Logging;
using System;

namespace Infrastructure.Services.Cache
{
    public class CachedUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _decoratedUnitOfWork;
        private readonly CachedRepositoryFactory _cachedRepositoryFactory;

        public CachedUnitOfWork(
            IUnitOfWork decoratedUnitOfWork,
            ICacheService cacheService,
            ILoggerFactory loggerFactory)
        {
            _decoratedUnitOfWork = decoratedUnitOfWork;
            _cachedRepositoryFactory = new CachedRepositoryFactory(
                decoratedUnitOfWork, 
                cacheService, 
                loggerFactory);
        }

        public IRepository<T, TId> GetRepository<T, TId>() 
            where T : Domain.Entities.BaseEntity<TId> 
            where TId : struct
        {
            // Utilizamos la fábrica para determinar si necesitamos un repositorio con caché
            return _cachedRepositoryFactory.GetCachedRepository<T, TId>();
        }

        public async Task<int> CommitAsync(CancellationToken ct = default)
        {
            // Simplemente delegamos al UnitOfWork original
            return await _decoratedUnitOfWork.CommitAsync(ct);
        }
    }
}
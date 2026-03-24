using Domain.Common;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Reflection;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Cache;

namespace Infrastructure.Repositories.Cache
{
    /// <summary>
    /// Decorator that adds an in-memory cache layer to repository read operations.
    /// Write operations invalidate affected cache entries to maintain consistency.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <typeparam name="TId">Primary key type.</typeparam>
    public class CachedRepository<T, TId> : IRepository<T, TId>
        where T : BaseEntity<TId>
        where TId : struct
    {
        private readonly IRepository<T, TId> _decoratedRepository;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CachedRepository<T, TId>> _logger;
        private readonly int _cacheTimeMinutes;
        private readonly string _entityName;

        /// <summary>
        /// Circuit breaker flag. When false, all cache operations are bypassed
        /// and queries execute directly against the database.
        /// </summary>
        private bool _isCacheHealthy = true;

        public CachedRepository(
            IRepository<T, TId> decoratedRepository,
            ICacheService cacheService,
            ILogger<CachedRepository<T, TId>> logger,
            int cacheTimeMinutes = 30)
        {
            _decoratedRepository = decoratedRepository;
            _cacheService = cacheService;
            _logger = logger;
            _cacheTimeMinutes = cacheTimeMinutes;
            _entityName = typeof(T).Name;
        }

        public async Task AddAsync(T entity)
        {
            await _decoratedRepository.AddAsync(entity);
            await InvalidateListCachesSafeAsync();
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _decoratedRepository.AddRangeAsync(entities);
            await InvalidateListCachesSafeAsync();
        }

        public async Task DeleteAsync(T entity)
        {
            await _decoratedRepository.DeleteAsync(entity);
            await InvalidateEntityCachesAsync(entity.Id);
        }

        public Task<IEnumerable<T>> GetAllAsync()
        {
            return ExecuteWithFallbackAsync<IEnumerable<T>>(
                $"{_entityName}_list_all",
                () => _decoratedRepository.GetAllAsync());
        }

        public Task<IEnumerable<T>> GetAllAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int? pageNumber = null,
            int? pageSize = null)
        {
            string filterHash = filter != null ? GenerateExpressionKey(filter) : "nofilter";
            string orderByHash = orderBy?.ToString()?.GetHashCode().ToString() ?? "noorder";
            string paginationKey = (pageNumber.HasValue && pageSize.HasValue)
                ? $"_page{pageNumber}_size{pageSize}"
                : "";

            string cacheKey = $"{_entityName}_list_{filterHash}_{orderByHash}{paginationKey}";

            return ExecuteWithFallbackAsync<IEnumerable<T>>(
                cacheKey,
                () => _decoratedRepository.GetAllAsync(filter, orderBy, pageNumber, pageSize));
        }

        public Task<PaginatedResult<T>> GetAllWithPaginationAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int pageNumber = 1,
            int pageSize = 10)
        {
            string filterHash = filter != null ? GenerateExpressionKey(filter) : "nofilter";
            string orderByHash = orderBy?.ToString()?.GetHashCode().ToString() ?? "noorder";
            string cacheKey = $"{_entityName}_list_paginated_{filterHash}_{orderByHash}_page{pageNumber}_size{pageSize}";

            return ExecuteWithFallbackAsync<PaginatedResult<T>>(
                cacheKey,
                () => _decoratedRepository.GetAllWithPaginationAsync(filter, orderBy, pageNumber, pageSize));
        }

        public Task<T?> GetByIdAsync(TId id)
        {
            string cacheKey = $"{_entityName}_{id}";
            return ExecuteWithFallbackAsync<T?>(
                cacheKey,
                () => _decoratedRepository.GetByIdAsync(id));
        }

        public Task<T?> GetFirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken)
        {
            string predicateHash = GenerateExpressionKey(predicate);
            string cacheKey = $"{_entityName}_first_{predicateHash}";

            return ExecuteWithFallbackAsync<T?>(
                cacheKey,
                () => _decoratedRepository.GetFirstOrDefaultAsync(predicate, cancellationToken));
        }

        public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            string filterHash = predicate != null ? GenerateExpressionKey(predicate) : "nofilter";
            string cacheKey = $"{_entityName}_count_{filterHash}";

            return ExecuteWithFallbackAsync<int>(
                cacheKey,
                () => _decoratedRepository.CountAsync(predicate, cancellationToken));
        }

        public Task<bool> AnyAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            string filterHash = predicate != null ? GenerateExpressionKey(predicate) : "nofilter";
            string cacheKey = $"{_entityName}_any_{filterHash}";

            return ExecuteWithFallbackAsync<bool>(
                cacheKey,
                () => _decoratedRepository.AnyAsync(predicate, cancellationToken));
        }

        public async Task UpdateAsync(T entity)
        {
            await _decoratedRepository.UpdateAsync(entity);
            await InvalidateEntityCachesAsync(entity.Id);
        }

        public async Task UpdatePartialAsync(T entity, Expression<Func<T, object?>>[] updatedProperties)
        {
            await _decoratedRepository.UpdatePartialAsync(entity, updatedProperties);
            await InvalidateEntityCachesAsync(entity.Id);
        }

        /// <summary>
        /// Appends to the cache key the values of fields captured in the lambda closure.
        /// This prevents cache collisions when the same query logic references different
        /// external values (e.g., user IDs, role filters).
        /// </summary>
        /// <remarks>
        /// System.Text.Json cannot serialize System.Type instances. When a closure captures
        /// a Type object, its FullName is used instead of JSON serialization.
        /// </remarks>
        private string AugmentCacheKey(string baseKey, Delegate dataOperation)
        {
            var sb = new StringBuilder(baseKey);
            var target = dataOperation.Target;

            if (target != null)
            {
                foreach (var f in target.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    var val = f.GetValue(target);
                    if (val == null || val is IRepository<T, TId>) continue;

                    // Types cannot be JSON-serialized; use FullName as a stable identifier.
                    string serializedVal = val is Type t ? t.FullName ?? t.Name : JsonSerializer.Serialize(val);
                    sb.Append('|').Append(serializedVal);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Executes a potentially cached operation with graceful degradation.
        /// If the cache is unhealthy or an error occurs, the operation runs directly
        /// against the database without caching the result.
        /// </summary>
        private async Task<TResult> ExecuteWithFallbackAsync<TResult>(string cacheKey, Func<Task<TResult>> dataOperation)
        {
            if (!_isCacheHealthy)
            {
                _logger.LogWarning("Cache disabled for {EntityType}, executing direct query", _entityName);
                return await dataOperation();
            }

            try
            {
                // Enrich the key with closure-captured values to prevent cross-request pollution.
                var fullKey = AugmentCacheKey(cacheKey, dataOperation);
                return await _cacheService.GetOrCreateAsync(fullKey, dataOperation, _cacheTimeMinutes);
            }
            catch (Exception ex)
            {
                // Log the cache failure but do not throw — degrade to direct database access.
                _logger.LogError(ex,
                    "Cache access failed for {EntityType} with key {CacheKey}. Falling back to direct query.",
                    _entityName, cacheKey);
                await TryResetCacheAsync();
                return await dataOperation();
            }
        }

        /// <summary>
        /// Resets the cache for this entity by clearing all its entries and
        /// temporarily disabling the cache layer via circuit breaker.
        /// After 5 minutes, the cache is automatically re-enabled.
        /// </summary>
        private async Task TryResetCacheAsync()
        {
            _isCacheHealthy = false;

            try
            {
                await _cacheService.RemoveByPatternAsync($"{_entityName}_*");
                _logger.LogWarning("Cache reset for entity {EntityType}", _entityName);

                // Schedule cache re-enablement after a cooldown period.
                _ = Task.Delay(TimeSpan.FromMinutes(5))
                    .ContinueWith(_ => {
                        _isCacheHealthy = true;
                        _logger.LogInformation("Cache re-enabled for entity {EntityType}", _entityName);
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset cache for entity {EntityType}", _entityName);
            }
        }

        /// <summary>
        /// Generates a stable string key from a LINQ expression by extracting
        /// both the expression structure and the constant values it contains.
        /// </summary>
        /// <remarks>
        /// Constant Type objects are serialized using FullName instead of JSON
        /// serialization to avoid NotSupportedException at runtime.
        /// </remarks>
        private static string GenerateExpressionKey(Expression expression)
        {
            var sb = new StringBuilder();
            var constants = new List<object>();
            var collector = new ConstantCollector(constants);
            collector.Visit(expression);
            sb.Append(expression.ToString());

            foreach (var constant in constants)
            {
                sb.Append('|');
                // Types cannot be serialized by System.Text.Json; use FullName as fallback.
                string serializedConstant = constant is Type t ? t.FullName ?? t.Name : JsonSerializer.Serialize(constant);
                sb.Append(serializedConstant);
            }
            return sb.ToString().GetHashCode().ToString();
        }

        /// <summary>
        /// Traverses a LINQ expression tree to collect all constant values
        /// embedded within it, enabling cache key generation that accounts
        /// for the actual data values used in the query.
        /// </summary>
        private class ConstantCollector : ExpressionVisitor
        {
            private readonly List<object> _constants;
            public ConstantCollector(List<object> constants) => _constants = constants;

            protected override Expression VisitConstant(ConstantExpression node)
            {
                if (node.Value != null)
                    _constants.Add(node.Value);
                return base.VisitConstant(node);
            }
        }

        /// <summary>
        /// Invalidates all cache entries for a specific entity instance
        /// (by ID) and all associated list/first/count/any caches.
        /// Errors during invalidation are swallowed to prevent write operations from failing.
        /// </summary>
        private async Task InvalidateEntityCachesAsync(TId entityId)
        {
            try
            {
                await _cacheService.RemoveAsync($"{_entityName}_{entityId}");
                await _cacheService.RemoveByPatternAsync($"{_entityName}_list*");
                await _cacheService.RemoveByPatternAsync($"{_entityName}_first*");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Cache invalidation failed after updating {EntityType} with ID {EntityId}. Cache may contain stale data.",
                    _entityName, entityId);
                await TryResetCacheAsync();
            }
        }

        /// <summary>
        /// Invalidates all list caches for the current entity.
        /// Used after Add and AddRange operations.
        /// Errors during invalidation are logged but not thrown.
        /// </summary>
        private async Task InvalidateListCachesSafeAsync()
        {
            try
            {
                await _cacheService.RemoveByPatternAsync($"{_entityName}_list*");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Cache invalidation failed after adding entities of type {EntityType}",
                    _entityName);
                await TryResetCacheAsync();
            }
        }
    }
}

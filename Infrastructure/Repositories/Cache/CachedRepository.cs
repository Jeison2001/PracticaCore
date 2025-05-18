using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Cache;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Reflection;

namespace Infrastructure.Repositories.Cache
{
    /// <summary>
    /// Decorador para repositorios que añade caché a las operaciones de lectura
    /// </summary>
    public class CachedRepository<T, TId> : IRepository<T, TId> 
        where T : BaseEntity<TId> 
        where TId : struct
    {
        private readonly IRepository<T, TId> _decoratedRepository;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CachedRepository<T, TId>> _logger;
        private readonly int _cacheTimeMinutes;
        private readonly string _entityName;
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
            try
            {
                // Invalidamos la caché de listados cuando se añade una entidad nueva
                await _cacheService.RemoveByPatternAsync($"{_entityName}_list*");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al invalidar caché después de añadir entidad {entityType} con ID {entityId}",
                    _entityName, entity.Id);
                await TryResetCacheAsync();
            }
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _decoratedRepository.AddRangeAsync(entities);
            try
            {
                // Invalidar la caché de listados al agregar múltiples entidades
                await _cacheService.RemoveByPatternAsync($"{_entityName}_list*");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al invalidar caché después de agregar múltiples entidades de {entityType}", _entityName);
                await TryResetCacheAsync();
            }
        }

        public async Task DeleteAsync(T entity)
        {
            await _decoratedRepository.DeleteAsync(entity);
            try
            {
                // Invalidamos la caché cuando se elimina una entidad
                await _cacheService.RemoveAsync($"{_entityName}_{entity.Id}");
                await _cacheService.RemoveByPatternAsync($"{_entityName}_list*");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al invalidar caché después de eliminar entidad {entityType} con ID {entityId}",
                    _entityName, entity.Id);
                await TryResetCacheAsync();
            }
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
            // Clave base sin discriminator
            string filterHash = filter != null ? GenerateExpressionKey(filter) : "nofilter";
            string orderByHash = "noorder";
            if (orderBy != null)
            {
                var orderByToString = orderBy.ToString();
                if (orderByToString != null)
                {
                    orderByHash = orderByToString.GetHashCode().ToString();
                }
            }
            string paginationKey = (pageNumber.HasValue && pageSize.HasValue) ? $"_page{pageNumber}_size{pageSize}" : "";
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
            string orderByHash = "noorder";
            if (orderBy != null)
            {
                var orderByToString = orderBy.ToString();
                if (orderByToString != null)
                {
                    orderByHash = orderByToString.GetHashCode().ToString();
                }
            }
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

        public async Task UpdateAsync(T entity)
        {
            await _decoratedRepository.UpdateAsync(entity);
            try
            {
                // Invalidamos la caché cuando se actualiza una entidad
                await _cacheService.RemoveAsync($"{_entityName}_{entity.Id}");
                await _cacheService.RemoveByPatternAsync($"{_entityName}_list*");
                await _cacheService.RemoveByPatternAsync($"{_entityName}_first*");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al invalidar caché después de actualizar entidad {entityType} con ID {entityId}",
                    _entityName, entity.Id);
                await TryResetCacheAsync();
            }
        }

        public async Task UpdatePartialAsync(T entity, Expression<Func<T, object>>[] updatedProperties)
        {
            await _decoratedRepository.UpdatePartialAsync(entity, updatedProperties);
            try
            {
                // Invalidamos la caché cuando se actualiza una entidad
                await _cacheService.RemoveAsync($"{_entityName}_{entity.Id}");
                await _cacheService.RemoveByPatternAsync($"{_entityName}_list*");
                 await _cacheService.RemoveByPatternAsync($"{_entityName}_first*");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al invalidar caché después de actualizar entidad {entityType} con ID {entityId}",
                    _entityName, entity.Id);
                await TryResetCacheAsync();
            }
        }

        // Helper para enriquecer la clave de caché con valores capturados en el cierre
        private string AugmentCacheKey(string baseKey, Delegate dataOperation)
        {
            var sb = new StringBuilder(baseKey);
            var target = dataOperation.Target;
            if (target != null)
            {
                var fields = target.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var f in fields)
                {
                    var val = f.GetValue(target);
                    if (val == null || val is IRepository<T, TId>) continue;
                    sb.Append('|').Append(JsonSerializer.Serialize(val));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Ejecuta una operación con caché con degradación elegante a la operación directa en caso de error
        /// </summary>
        private async Task<TResult> ExecuteWithFallbackAsync<TResult>(string cacheKey, Func<Task<TResult>> dataOperation)
        {
            if (!_isCacheHealthy)
            {
                _logger.LogWarning("Caché desactivado para {entityType}, ejecutando operación directa", _entityName);
                return await dataOperation();
            }

            try
            {
                // Enriquecer clave con valores del closure para evitar colisiones
                var fullKey = AugmentCacheKey(cacheKey, dataOperation);
                return await _cacheService.GetOrCreateAsync(fullKey, dataOperation, _cacheTimeMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al acceder a caché para {entityType} con clave {key}. Usando operación directa.",
                    _entityName, cacheKey);
                await TryResetCacheAsync();
                return await dataOperation();
            }
        }

        /// <summary>
        /// Intenta reiniciar el caché cuando se detectan problemas
        /// </summary>
        private async Task TryResetCacheAsync()
        {
            _isCacheHealthy = false;
            
            try
            {
                // Limpiar todas las entradas de caché para esta entidad
                await _cacheService.RemoveByPatternAsync($"{_entityName}_*");
                _logger.LogWarning("Caché reiniciado para la entidad {entityType}", _entityName);
                
                // Programar la reactivación del caché después de un tiempo
                _ = Task.Delay(TimeSpan.FromMinutes(5))
                    .ContinueWith(_ => {
                        _isCacheHealthy = true;
                        _logger.LogInformation("Caché reactivado para la entidad {entityType}", _entityName);
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al intentar reiniciar el caché para {entityType}", _entityName);
            }
        }

        // Agregar métodos para generar claves únicas de expresiones incluyendo valores capturados
        private static string GenerateExpressionKey(Expression expression)
        {
            var sb = new StringBuilder();
            var constants = new List<object>();
            var collector = new ConstantCollector(constants);
            collector.Visit(expression);
            sb.Append(expression.ToString());
            foreach (var constant in constants)
            {
                sb.Append("|");
                sb.Append(JsonSerializer.Serialize(constant));
            }
            return sb.ToString().GetHashCode().ToString();
        }

        private class ConstantCollector : ExpressionVisitor
        {
            private readonly List<object> _constants;
            public ConstantCollector(List<object> constants) => _constants = constants;
            protected override Expression VisitConstant(ConstantExpression node)
            {
                if (node.Value != null)
                {
                    _constants.Add(node.Value);
                }
                return base.VisitConstant(node);
            }
        }
    }
}
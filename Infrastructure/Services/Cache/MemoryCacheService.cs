using Domain.Interfaces.Cache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Infrastructure.Services.Cache
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ConcurrentDictionary<string, bool> _keys;
        private readonly int _defaultExpirationMinutes;
        private readonly ILogger<MemoryCacheService> _logger;

        public MemoryCacheService(
            IMemoryCache memoryCache, 
            ILogger<MemoryCacheService> logger,
            int defaultExpirationMinutes = 30)
        {
            _memoryCache = memoryCache;
            _keys = new ConcurrentDictionary<string, bool>();
            _defaultExpirationMinutes = defaultExpirationMinutes;
            _logger = logger;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, int? absoluteExpirationMinutes = null)
        {
            try
            {
                // Usamos la versión segura para tipos que pueden ser nulos
                if (_memoryCache.TryGetValue<T>(key, out var cachedItem) && cachedItem != null)
                {
                    _logger.LogDebug("Cache hit para la clave: {key}", key);
                    return cachedItem;
                }

                _logger.LogDebug("Cache miss para la clave: {key}, obteniendo valor de origen", key);
                var item = await factory();

                // Si el valor es nulo, no lo almacenamos en caché
                if (item == null)
                {
                    _logger.LogWarning("Valor nulo obtenido para la clave: {key}, no se almacenará en caché", key);
                    return default!;
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(absoluteExpirationMinutes ?? _defaultExpirationMinutes))
                    // Evento que se dispara cuando el elemento es eliminado de la caché
                    .RegisterPostEvictionCallback((key, value, reason, state) =>
                    {
                        _logger.LogDebug("Elemento con clave {key} eliminado de caché. Razón: {reason}", key, reason);
                        _keys.TryRemove(key.ToString(), out _);
                    });

                _memoryCache.Set(key, item, cacheEntryOptions);
                _keys.TryAdd(key, true);
                
                _logger.LogDebug("Valor almacenado en caché con clave: {key} y expiración en {minutes} minutos", 
                    key, absoluteExpirationMinutes ?? _defaultExpirationMinutes);

                return item;
            }
            catch (Exception ex)
            {
                // Log detallado del error
                _logger.LogError(ex, "Error al interactuar con caché para la clave: {key}. Obteniendo valor directamente.", key);
                
                // Si hay cualquier error con la caché, recurrimos a la operación directa
                return await factory();
            }
        }

        public Task RemoveAsync(string key)
        {
            try 
            {
                _logger.LogDebug("Eliminando entrada de caché con clave: {key}", key);
                _memoryCache.Remove(key);
                _keys.TryRemove(key, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar entrada de caché con clave: {key}", key);
                // Continuamos incluso si hay error, ya que el impacto es bajo
            }
            return Task.CompletedTask;
        }

        public Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                _logger.LogDebug("Eliminando entradas de caché que coinciden con el patrón: {pattern}", pattern);
                var regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var removedCount = 0;
                
                foreach (var key in _keys.Keys)
                {
                    if (regex.IsMatch(key))
                    {
                        _memoryCache.Remove(key);
                        if (_keys.TryRemove(key, out _))
                        {
                            removedCount++;
                        }
                    }
                }
                
                _logger.LogDebug("Se eliminaron {count} entradas de caché con el patrón: {pattern}", removedCount, pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar entradas de caché con patrón: {pattern}", pattern);
                // Continuamos incluso si hay error, ya que el impacto es bajo
            }
            
            return Task.CompletedTask;
        }
        
        public Task ClearAllAsync()
        {
            try
            {
                _logger.LogWarning("Limpiando toda la caché");
                foreach (var key in _keys.Keys)
                {
                    _memoryCache.Remove(key);
                }
                _keys.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al limpiar toda la caché");
            }
            
            return Task.CompletedTask;
        }
    }
}
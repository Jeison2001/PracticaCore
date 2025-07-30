using Domain.Entities;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services
{
    /// <summary>
    /// Dispatcher genérico que resuelve automáticamente el handler apropiado.
    /// Sigue Open/Closed Principle - agregar nuevos handlers no requiere modificar este código.
    /// </summary>
    public class NotificationDispatcher : INotificationDispatcher
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<NotificationDispatcher> _logger;

        public NotificationDispatcher(IServiceScopeFactory serviceScopeFactory, ILogger<NotificationDispatcher> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task DispatchEntityChangeAsync<T, TId>(T oldEntity, T newEntity, CancellationToken cancellationToken = default) 
            where T : BaseEntity<TId> 
            where TId : struct
        {
            try
            {
                // ✅ CRÍTICO: Usar IServiceScopeFactory para crear scope independiente
                using var scope = _serviceScopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetService<IEntityChangeHandler<T, TId>>();
                
                if (handler != null)
                {
                    _logger.LogDebug("Dispatching change notification for {EntityType} ID: {Id}", typeof(T).Name, newEntity.Id);
                    await handler.HandleChangeAsync(oldEntity, newEntity, cancellationToken);
                }
                else
                {
                    _logger.LogDebug("No handler configured for entity type: {EntityType}", typeof(T).Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching change notification for entity type: {EntityType}, ID: {Id}", 
                    typeof(T).Name, newEntity.Id);
                // No re-throw para evitar que falle la operación principal
            }
        }

        public async Task DispatchEntityCreationAsync<T, TId>(T entity, CancellationToken cancellationToken = default) 
            where T : BaseEntity<TId> 
            where TId : struct
        {
            try
            {
                // ✅ CRÍTICO: Usar IServiceScopeFactory para crear scope independiente
                using var scope = _serviceScopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetService<IEntityChangeHandler<T, TId>>();
                
                if (handler != null)
                {
                    _logger.LogDebug("Dispatching creation notification for {EntityType} ID: {Id}", typeof(T).Name, entity.Id);
                    await handler.HandleCreationAsync(entity, cancellationToken);
                }
                else
                {
                    _logger.LogDebug("No handler configured for entity type: {EntityType}", typeof(T).Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching creation notification for entity type: {EntityType}, ID: {Id}", 
                    typeof(T).Name, entity.Id);
                // No re-throw para evitar que falle la operación principal
            }
        }
    }
}

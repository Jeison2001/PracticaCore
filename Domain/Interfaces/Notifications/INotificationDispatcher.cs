using Domain.Entities;
using Domain.Interfaces.Registration;

namespace Domain.Interfaces.Notifications
{
    /// <summary>
    /// Dispatcher genérico que resuelve y ejecuta el handler apropiado para cada tipo de entidad.
    /// Implementa Open/Closed Principle - agregar nuevos tipos no requiere modificar este código.
    /// </summary>
    public interface INotificationDispatcher : IScopedService
    {
        Task DispatchEntityChangeAsync<T, TId>(T oldEntity, T newEntity, CancellationToken cancellationToken = default) 
            where T : BaseEntity<TId> 
            where TId : struct;
        Task DispatchEntityCreationAsync<T, TId>(T entity, CancellationToken cancellationToken = default) 
            where T : BaseEntity<TId> 
            where TId : struct;
    }
}

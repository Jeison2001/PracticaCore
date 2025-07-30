using Domain.Entities;
using Domain.Interfaces.Registration;

namespace Domain.Interfaces.Notifications
{
    /// <summary>
    /// Handler específico para cambios en un tipo de entidad.
    /// Implementa Single Responsibility Principle - cada handler maneja un solo tipo.
    /// </summary>
    public interface IEntityChangeHandler<T, TId> : IScopedService 
        where T : BaseEntity<TId> 
        where TId : struct
    {
        Task HandleChangeAsync(T oldEntity, T newEntity, CancellationToken cancellationToken = default);
        Task HandleCreationAsync(T entity, CancellationToken cancellationToken = default);
    }
}

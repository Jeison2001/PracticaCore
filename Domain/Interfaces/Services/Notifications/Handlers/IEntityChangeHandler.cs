using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications.Handlers;

/// <summary>
/// Handler para detectar cambios de estado en entidades y encolar notificaciones.
/// Capture: HandleCreationAsync (nueva entidad) y HandleChangeAsync (cambio de estado).
/// Cada implementación es para un tipo de entidad específico (Proposal, InscriptionModality, etc.).
/// </summary>
public interface IEntityChangeHandler<T, TId> : IScopedService 
    where T : BaseEntity<TId> 
    where TId : struct
{
    Task HandleChangeAsync(T oldEntity, T newEntity, CancellationToken cancellationToken = default);
    Task HandleCreationAsync(T entity, CancellationToken cancellationToken = default);
}

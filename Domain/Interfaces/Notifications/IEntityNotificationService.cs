using Domain.Interfaces.Registration;

namespace Domain.Interfaces.Notifications
{
    /// <summary>
    /// Servicio simplificado para notificaciones automáticas de cambios en entidades
    /// </summary>
    public interface IEntityNotificationService : IScopedService
    {
        /// <summary>
        /// Procesa notificaciones automáticas para cambios en InscriptionModality
        /// </summary>
        /// <param name="oldEntity">Estado anterior de la entidad</param>
        /// <param name="newEntity">Estado nuevo de la entidad</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        Task ProcessInscriptionModalityChangesAsync(object oldEntity, object newEntity, CancellationToken cancellationToken = default);
    }
}

using Domain.Common;
using Domain.Interfaces.Registration;

namespace Domain.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de notificaciones
    /// </summary>
    public interface INotificationService : IScopedService
    {
        /// <summary>
        /// Envía una notificación por correo electrónico
        /// </summary>
        /// <param name="notification">Datos de la notificación a enviar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si el envío fue exitoso, false en caso contrario</returns>
        Task<bool> SendEmailAsync(EmailNotification notification, CancellationToken cancellationToken = default);
    }
}

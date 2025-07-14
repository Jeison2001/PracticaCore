using Domain.Common;
using Domain.Interfaces.Registration;
using Domain.Entities;

namespace Domain.Interfaces
{
    /// <summary>
    /// Servicio para manejar eventos de notificación automáticos
    /// </summary>
    public interface IEmailNotificationEventService : IScopedService
    {
        /// <summary>
        /// Procesa un evento y envía las notificaciones correspondientes
        /// </summary>
        /// <param name="eventName">Nombre del evento (ej: "PROPOSAL_CREATED")</param>
        /// <param name="eventData">Datos del evento para reemplazar en plantillas</param>
        /// <param name="entityContext">Contexto de la entidad relacionada para determinar destinatarios</param>
        Task ProcessEventAsync(string eventName, Dictionary<string, object> eventData, object? entityContext = null);

        /// <summary>
        /// Verifica si existe configuración activa para un evento
        /// </summary>
        Task<bool> HasActiveConfigurationAsync(string eventName);
    }

    /// <summary>
    /// Servicio para resolver destinatarios basado en reglas
    /// </summary>
    public interface IEmailRecipientResolverService : IScopedService
    {
        /// <summary>
        /// Resuelve los destinatarios según las reglas configuradas
        /// </summary>
        Task<EmailRecipientsResult> ResolveRecipientsAsync(
            List<EmailRecipientRule> rules, 
            Dictionary<string, object> eventData, 
            object? entityContext = null);
    }

    /// <summary>
    /// Resultado de la resolución de destinatarios
    /// </summary>
    public class EmailRecipientsResult
    {
        public List<string> To { get; set; } = new();
        public List<string> Cc { get; set; } = new();
        public List<string> Bcc { get; set; } = new();
    }
}

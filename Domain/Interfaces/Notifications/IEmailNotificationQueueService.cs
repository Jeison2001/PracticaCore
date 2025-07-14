using Domain.Interfaces.Registration;

namespace Domain.Interfaces.Notifications
{
    /// <summary>
    /// Servicio para encolar tareas de notificaciones de manera asíncrona
    /// </summary>
    public interface IEmailNotificationQueueService : IScopedService
    {
        /// <summary>
        /// Encola una tarea de procesamiento de evento de notificación
        /// </summary>
        /// <param name="eventName">Nombre del evento a procesar</param>
        /// <param name="eventData">Datos del evento</param>
        /// <returns>ID del trabajo encolado</returns>
        string EnqueueEventNotification(string eventName, Dictionary<string, object> eventData);

        /// <summary>
        /// Encola una tarea de envío directo de email
        /// </summary>
        /// <param name="to">Destinatario</param>
        /// <param name="subject">Asunto</param>
        /// <param name="body">Cuerpo del mensaje</param>
        /// <param name="isHtml">Si el cuerpo es HTML</param>
        /// <param name="cc">Destinatarios en copia (opcional)</param>
        /// <param name="bcc">Destinatarios en copia oculta (opcional)</param>
        /// <returns>ID del trabajo encolado</returns>
        string EnqueueDirectEmail(string to, string subject, string body, bool isHtml = true, 
            string[]? cc = null, string[]? bcc = null);

        /// <summary>
        /// Procesa un evento de notificación (método para ejecutar en background)
        /// </summary>
        /// <param name="eventName">Nombre del evento</param>
        /// <param name="eventData">Datos del evento</param>
        /// <returns>Resultado del procesamiento</returns>
        Task ProcessEventNotificationAsync(string eventName, Dictionary<string, object> eventData);

        /// <summary>
        /// Envía un email directo (método para ejecutar en background)
        /// </summary>
        /// <param name="to">Destinatario</param>
        /// <param name="subject">Asunto</param>
        /// <param name="body">Cuerpo del mensaje</param>
        /// <param name="isHtml">Si el cuerpo es HTML</param>
        /// <param name="cc">Destinatarios en copia</param>
        /// <param name="bcc">Destinatarios en copia oculta</param>
        /// <returns>Resultado del envío</returns>
        Task SendDirectEmailAsync(string to, string subject, string body, bool isHtml = true, 
            string[]? cc = null, string[]? bcc = null);
    }
}

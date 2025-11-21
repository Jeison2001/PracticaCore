using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications;

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


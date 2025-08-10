using Domain.Interfaces.Registration;

namespace Domain.Interfaces.Notifications
{
    /// <summary>
    /// Constructor de datos de eventos para notificaciones de anteproyecto
    /// </summary>
    public interface IPreliminaryProjectEventDataBuilder : IScopedService
    {
        /// <summary>
        /// Construye datos de evento para notificaciones de anteproyecto
        /// </summary>
        Task<Dictionary<string, object>> BuildPreliminaryProjectEventDataAsync(int preliminaryProjectId, string eventType);
    }
}

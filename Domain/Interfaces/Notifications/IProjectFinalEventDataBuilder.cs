using Domain.Interfaces.Registration;

namespace Domain.Interfaces.Notifications
{
    /// <summary>
    /// Constructor de datos de eventos para notificaciones de proyecto final
    /// </summary>
    public interface IProjectFinalEventDataBuilder : IScopedService
    {
        /// <summary>
        /// Construye datos de evento para notificaciones de proyecto final
        /// </summary>
        Task<Dictionary<string, object>> BuildProjectFinalEventDataAsync(int projectFinalId, string eventType);
    }
}

using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications.Builders
{
    /// <summary>
    /// Construye placeholders para emails de Proyecto Final: título, estudiantes,
    /// estado, fechas y observaciones (misma estructura que PreliminaryProject).
    /// </summary>
    public interface IProjectFinalEventDataBuilder : IScopedService
    {
        /// <summary>
        /// Construye datos de evento para notificaciones de proyecto final
        /// </summary>
        Task<Dictionary<string, object>> BuildProjectFinalEventDataAsync(int projectFinalId, string eventType);
    }
}

using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications.Builders
{
    /// <summary>
    /// Construye placeholders para emails de Anteproyecto: título del proyecto,
    /// estudiantes, estado, fecha de Submission, observaciones y evaluación.
    /// </summary>
    public interface IPreliminaryProjectEventDataBuilder : IScopedService
    {
        /// <summary>
        /// Construye datos de evento para notificaciones de anteproyecto
        /// </summary>
        Task<Dictionary<string, object>> BuildPreliminaryProjectEventDataAsync(int preliminaryProjectId, string eventType);
    }
}

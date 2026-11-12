using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications.Builders
{
    /// <summary>
    /// Construye placeholders para emails de Sustentación (ProjectPresentation):
    /// título, estudiantes, fecha programada, fecha real, modalidad, lugar/enlace, resultado y observaciones.
    /// </summary>
    public interface IProjectPresentationEventDataBuilder : IScopedService
    {
        Task<Dictionary<string, object>> BuildProjectPresentationEventDataAsync(int presentationId, string eventType);
    }
}

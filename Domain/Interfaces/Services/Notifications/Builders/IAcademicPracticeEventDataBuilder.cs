using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications.Builders
{
    /// <summary>
    /// Builder para construir datos de eventos de notificaciones para Práctica Académica
    /// </summary>
    public interface IAcademicPracticeEventDataBuilder : IScopedService
    {
        Task<Dictionary<string, object>> BuildAcademicPracticeEventDataAsync(int academicPracticeId, string eventType);
    }
}

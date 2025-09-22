using Domain.Interfaces.Registration;

namespace Domain.Interfaces.Notifications
{
    /// <summary>
    /// Builder para construir datos de eventos de notificaciones para Práctica Académica
    /// </summary>
    public interface IAcademicPracticeEventDataBuilder : IScopedService
    {
        Task<Dictionary<string, object>> BuildAcademicPracticeEventDataAsync(int academicPracticeId, string eventType);
    }
}

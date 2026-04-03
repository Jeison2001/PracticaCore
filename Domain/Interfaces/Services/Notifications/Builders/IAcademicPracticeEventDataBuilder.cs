using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications.Builders
{
    /// <summary>
    /// Construye placeholders para emails de Práctica Académica: título, institución,
    /// fase, estado, estudiantes y fechas de aprobación/desarrollo.
    /// </summary>
    public interface IAcademicPracticeEventDataBuilder : IScopedService
    {
        Task<Dictionary<string, object>> BuildAcademicPracticeEventDataAsync(int academicPracticeId, string eventType);
    }
}

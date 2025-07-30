using Domain.Interfaces.Registration;

namespace Domain.Interfaces.Notifications
{
    /// <summary>
    /// Builder específico para datos de eventos de InscriptionModality.
    /// Single Responsibility: Solo construye datos para InscriptionModality.
    /// </summary>
    public interface IInscriptionEventDataBuilder : IScopedService
    {
        Task<Dictionary<string, object>> BuildInscriptionEventDataAsync(int inscriptionId, string eventType);
        Task<Dictionary<string, object>> BuildBasicInscriptionDataAsync(int inscriptionId, int modalityId, int academicPeriodId, IEnumerable<int> studentIds);
    }
}

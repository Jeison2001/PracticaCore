using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications.Builders
{
    /// <summary>
    /// Construye placeholders para emails de InscriptionModality:
    /// estado, modalidad, período académico, estudiantes, fechas y comentarios.
    /// </summary>
    public interface IInscriptionEventDataBuilder : IScopedService
    {
        Task<Dictionary<string, object>> BuildInscriptionEventDataAsync(int inscriptionId, string eventType);
        Task<Dictionary<string, object>> BuildBasicInscriptionDataAsync(int inscriptionId, int modalityId, int academicPeriodId, IList<int> studentIds);
    }
}

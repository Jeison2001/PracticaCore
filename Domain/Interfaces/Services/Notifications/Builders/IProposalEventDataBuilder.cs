using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications.Builders
{
    /// <summary>
    /// Construye el diccionario de placeholders para emails de Proposal:
    /// título, objetivos, estado, línea de investigación, estudiantes, fechas.
    /// </summary>
    public interface IProposalEventDataBuilder : IScopedService
    {
        Task<Dictionary<string, object>> BuildProposalEventDataAsync(int proposalId, string eventType);
    }
}

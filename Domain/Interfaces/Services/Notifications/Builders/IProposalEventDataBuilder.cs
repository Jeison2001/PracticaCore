using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications.Builders
{
    /// <summary>
    /// Builder específico para datos de eventos de Proposal.
    /// Single Responsibility: Solo construye datos para Proposal.
    /// </summary>
    public interface IProposalEventDataBuilder : IScopedService
    {
        Task<Dictionary<string, object>> BuildProposalEventDataAsync(int proposalId, string eventType);
    }
}

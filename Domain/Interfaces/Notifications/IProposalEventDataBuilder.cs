using Domain.Interfaces.Registration;

namespace Domain.Interfaces.Notifications
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

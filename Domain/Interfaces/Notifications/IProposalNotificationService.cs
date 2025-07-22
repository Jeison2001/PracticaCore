using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Registration;

namespace Domain.Interfaces.Notifications
{
    public interface IProposalNotificationService : IScopedService
    {
        Task ProcessProposalEventAsync(Proposal proposal, StateStageEnum stateStage, CancellationToken cancellationToken = default);
    }
}

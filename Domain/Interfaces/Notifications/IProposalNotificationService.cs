using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces.Notifications
{
    public interface IProposalNotificationService
    {
        Task ProcessProposalEventAsync(Proposal proposal, StateStageEnum stateStage, CancellationToken cancellationToken = default);
    }
}

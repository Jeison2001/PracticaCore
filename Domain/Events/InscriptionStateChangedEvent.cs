using Domain.Common;

namespace Domain.Events
{
    public record InscriptionStateChangedEvent(
        int InscriptionModalityId, 
        int ModalityId, 
        int NewStateInscriptionId, 
        int TriggeredByUserId) : BaseEvent;
}

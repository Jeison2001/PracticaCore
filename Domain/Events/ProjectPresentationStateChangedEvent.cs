using Domain.Common;

namespace Domain.Events
{
    /// <summary>
    /// Se dispara cuando el estado de una Sustentación (ProjectPresentation) cambia.
    /// </summary>
    public record ProjectPresentationStateChangedEvent(
        int InscriptionModalityId,
        int OldStateStageId,
        int NewStateStageId,
        int TriggeredByUserId) : BaseEvent;
}

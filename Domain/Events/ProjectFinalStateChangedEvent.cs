using Domain.Common;

namespace Domain.Events
{
    /// <summary>
    /// Se dispara cuando el estado de un Proyecto Final cambia.
    /// Escucha sobre la tabla ProjectFinal.
    /// </summary>
    public record ProjectFinalStateChangedEvent(
        int InscriptionModalityId,
        int OldStateStageId,
        int NewStateStageId,
        int TriggeredByUserId) : BaseEvent;
}

using Domain.Common;

namespace Domain.Events
{
    /// <summary>
    /// Se dispara cuando el estado de una Práctica Académica cambia.
    /// Escucha sobre la tabla AcademicPractice.
    /// </summary>
    public record AcademicPracticeStateChangedEvent(
        int InscriptionModalityId,
        int ModalityId,
        int NewStateStageId,
        int OldStateStageId,
        int TriggeredByUserId) : BaseEvent;
}

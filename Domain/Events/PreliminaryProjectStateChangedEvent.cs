using Domain.Common;

namespace Domain.Events
{
    /// <summary>
    /// Se dispara cuando el estado de un AnteProyecto cambia (ej: de revisión a AP_APROBADO).
    /// Escucha sobre la tabla PreliminaryProject.
    /// </summary>
    public record PreliminaryProjectStateChangedEvent(
        int InscriptionModalityId,
        int NewStateStageId,
        int TriggeredByUserId) : BaseEvent;
}

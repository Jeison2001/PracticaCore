using Domain.Common;

namespace Domain.Events
{
    /// <summary>
    /// Se dispara cuando el estado de una Propuesta cambia (ej: de borrador a PERTINENTE).
    /// Escucha sobre la tabla Proposal.
    /// </summary>
    public record ProposalStateChangedEvent(
        int InscriptionModalityId,
        int NewStateStageId,
        int TriggeredByUserId) : BaseEvent;
}

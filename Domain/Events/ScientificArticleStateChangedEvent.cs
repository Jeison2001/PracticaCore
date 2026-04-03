using Domain.Common;

namespace Domain.Events
{
    /// <summary>
    /// Se dispara cuando el estado de un Artículo Científico cambia.
    /// Escucha sobre la tabla ScientificArticle.
    /// </summary>
    public record ScientificArticleStateChangedEvent(
        int InscriptionModalityId,
        int NewStateStageId,
        int OldStateStageId,
        int TriggeredByUserId) : BaseEvent;
}

using Domain.Common;

namespace Domain.Events
{
    /// <summary>
    /// Se dispara cuando se carga un Documento nuevo, para avanzar automáticamente
    /// el estado de las fases de Anteproyecto y Proyecto Final.
    /// </summary>
    public record DocumentUploadedEvent(
        int InscriptionModalityId,
        int DocumentTypeId,
        int ChangedByUserId) : BaseEvent;
}

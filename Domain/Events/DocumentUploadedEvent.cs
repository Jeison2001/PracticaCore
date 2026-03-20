using Domain.Common;

namespace Domain.Events
{
    /// <summary>
    /// Se dispara cuando se carga un Documento nuevo.
    /// Evento equivalente a los triggers trg_update_preliminary_project_on_document_upload
    /// y trg_update_project_final_on_document_upload.
    /// </summary>
    public record DocumentUploadedEvent(
        int InscriptionModalityId,
        int DocumentTypeId,
        int TriggeredByUserId) : BaseEvent;
}

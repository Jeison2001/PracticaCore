using Microsoft.AspNetCore.Http;

namespace Application.Shared.DTOs
{
    public class DocumentUpdateDto : BaseDto<int>
    {
        public int? IdInscriptionModality { get; set; }
        public int IdUploader { get; set; }
        public int IdDocumentType { get; set; }
        public string? CodeDocumentType { get; set; } // Renombrado: permite enviar el código
        public string? Name { get; set; }
        public IFormFile? File { get; set; } // Opcional en actualización
        public string? Version { get; set; }
        public string? DocumentState { get; set; }
        public int? IdDocumentOld { get; set; } // Nueva propiedad para referencia opcional al documento anterior
        // ...campos de auditoría heredados de BaseDto<int>...
    }
}

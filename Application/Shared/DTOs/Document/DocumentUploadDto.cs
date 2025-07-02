using Microsoft.AspNetCore.Http;

namespace Application.Shared.DTOs.Document
{    public class DocumentUploadDto : BaseDto<int>
    {
        public int? IdInscriptionModality { get; set; }
        public int IdDocumentType { get; set; }
        public string? CodeDocumentType { get; set; } // Permite enviar el código
        public string? Name { get; set; }
        public IFormFile File { get; set; } = null!;
        public string? Version { get; set; }
        public int? IdDocumentOld { get; set; } // Para referencia al documento anterior
        // ...campos de auditoría heredados de BaseDto<int>...
    }
}

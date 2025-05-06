using Microsoft.AspNetCore.Http;

namespace Application.Shared.DTOs
{
    public class DocumentUpdateDto : BaseDto<int>
    {
        public int? IdInscriptionModality { get; set; }
        public int IdUploader { get; set; }
        public int IdDocumentType { get; set; }
        public string? Name { get; set; }
        public IFormFile? File { get; set; } // Opcional en actualización
        public string? Version { get; set; }
        public string? DocumentState { get; set; }
        // ...campos de auditoría heredados de BaseDto<int>...
    }
}

using Microsoft.AspNetCore.Http;

namespace Application.Shared.DTOs
{
    public class DocumentUploadDto : BaseDto<int>
    {
        public int? IdInscriptionModality { get; set; }
        public int IdUploader { get; set; }
        public int IdDocumentType { get; set; }
        public IFormFile File { get; set; } = null!;
        public string? Version { get; set; }
        // ...campos de auditoría heredados de BaseDto<int>...
    }
}

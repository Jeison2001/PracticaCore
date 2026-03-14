using Application.Shared.DTOs.DocumentTypes;

namespace Application.Shared.DTOs.Documents
{    public record DocumentDto : BaseDto<int>
    {
        public int? IdInscriptionModality { get; set; }
        public int IdDocumentType { get; set; }
        public string? Name { get; set; } // Nombre simbólico asignado (ej: Carta Director)
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? Version { get; set; }
        public int? IdDocumentOld { get; set; }
        
        // DocumentType information
        public DocumentTypeDto? DocumentType { get; set; }
    }
}

namespace Domain.Entities
{
    public class Document : BaseEntity<int>
    {
        public int? IdInscriptionModality { get; set; }
        public int IdDocumentType { get; set; }
        public string? Name { get; set; } // Nombre simbólico asignado (ej: Carta Director)
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long FileSize { get; set; } // En bytes
        public string? Version { get; set; } // ej: '1.0', '1.1 Corregido', 'Final'
        public int? IdDocumentOld { get; set; } // Para versionamiento o reemplazo

        // Navigation properties
        public virtual DocumentType? DocumentType { get; set; }
        public virtual InscriptionModality? InscriptionModality { get; set; }
        public virtual Document? DocumentOld { get; set; }
    }
}
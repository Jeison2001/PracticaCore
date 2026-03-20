using Domain.Events;

namespace Domain.Entities
{
    public class Document : BaseEntity<int>
    {
        private int? _idInscriptionModality;
        public int? IdInscriptionModality
        {
            get => _idInscriptionModality;
            set
            {
                _idInscriptionModality = value;
                TryDispatchEvent();
            }
        }

        private int _idDocumentType;
        public int IdDocumentType
        {
            get => _idDocumentType;
            set
            {
                _idDocumentType = value;
                TryDispatchEvent();
            }
        }

        private void TryDispatchEvent()
        {
            if (_idInscriptionModality.HasValue && _idDocumentType != 0)
            {
                ClearDomainEvents();
                AddDomainEvent(new DocumentUploadedEvent(
                    _idInscriptionModality.Value, 
                    _idDocumentType, 
                    IdUserUpdatedAt ?? IdUserCreatedAt ?? 1));
            }
        }
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
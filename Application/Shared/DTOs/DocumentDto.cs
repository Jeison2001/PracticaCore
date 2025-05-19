namespace Application.Shared.DTOs
{
    public class DocumentDto : BaseDto<int>
    {
        public int? IdInscriptionModality { get; set; }
        public int IdUploader { get; set; }
        public int IdDocumentType { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? Version { get; set; }
        public string DocumentState { get; set; } = "CARGADO";
        public int? IdDocumentOld { get; set; }
    }
}
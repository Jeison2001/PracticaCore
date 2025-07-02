namespace Application.Shared.DTOs.DocumentType
{    public class DocumentTypeDto : BaseDto<int>
    {
        public int IdDocumentClass { get; set; }
        public int? IdStageModality { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}
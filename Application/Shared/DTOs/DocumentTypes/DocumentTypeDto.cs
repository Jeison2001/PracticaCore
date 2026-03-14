namespace Application.Shared.DTOs.DocumentTypes
{    public record DocumentTypeDto : BaseDto<int>
    {
        public int IdDocumentClass { get; set; }
        public int? IdStageModality { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}

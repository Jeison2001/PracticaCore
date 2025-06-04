namespace Domain.Entities
{    public class DocumentType : BaseEntity<int>
    {
        public int IdDocumentClass { get; set; }
        public int? IdStageModality { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public new int? IdUserCreatedAt { get; set; }

        // Navigation properties
        public virtual DocumentClass? DocumentClass { get; set; }
        public virtual StageModality? StageModality { get; set; }
    }
}
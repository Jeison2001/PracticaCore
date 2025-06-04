namespace Domain.Entities
{
    public class DocumentClass : BaseEntity<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public new int? IdUserCreatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<DocumentType>? DocumentTypes { get; set; }
    }
}

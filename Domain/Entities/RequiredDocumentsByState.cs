using System;

namespace Domain.Entities
{
    public class RequiredDocumentsByState : BaseEntity<int>
    {
        public int IdStateStage { get; set; }
        public int IdDocumentType { get; set; }
        public bool IsRequired { get; set; } = true;
        public int? OrderDisplay { get; set; }

        // Navigation properties
        public virtual StateStage StateStage { get; set; } = null!;
        public virtual DocumentType DocumentType { get; set; } = null!;
    }
}

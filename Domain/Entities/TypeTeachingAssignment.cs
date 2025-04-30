using System.Collections.Generic;

namespace Domain.Entities
{
    public class TypeTeachingAssignment : BaseEntity<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public new int? IdUserCreatedAt { get; set; }
        
        // Navigation property for relationship
        public virtual ICollection<TeachingAssignment> TeachingAssignments { get; set; } = new List<TeachingAssignment>();
    }
}
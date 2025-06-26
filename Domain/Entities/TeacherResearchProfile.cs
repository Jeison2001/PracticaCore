namespace Domain.Entities
{
    public class TeacherResearchProfile : BaseEntity<int>
    {
        public int IdUser { get; set; }
        public int IdResearchLine { get; set; }
        public int? IdResearchSubLine { get; set; }
        public string? ProfileDescription { get; set; }
        public new int? IdUserCreatedAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ResearchLine ResearchLine { get; set; } = null!;
        public virtual ResearchSubLine? ResearchSubLine { get; set; }
        public virtual ICollection<TeachingAssignment> TeachingAssignments { get; set; } = new List<TeachingAssignment>();
    }
}

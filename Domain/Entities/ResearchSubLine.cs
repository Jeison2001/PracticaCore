namespace Domain.Entities
{
    public class ResearchSubLine : BaseEntity<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public new int? IdUserCreatedAt { get; set; }
        public int IdResearchLine { get; set; }
        public virtual ResearchLine ResearchLine { get; set; }
        public virtual ICollection<ThematicArea> ThematicAreas { get; set; } = new List<ThematicArea>();
    }
}
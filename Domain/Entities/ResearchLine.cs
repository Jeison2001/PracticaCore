namespace Domain.Entities
{
    public class ResearchLine : BaseEntity<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public new int? IdUserCreatedAt { get; set; }
        public virtual ICollection<ResearchSubLine> ResearchSubLines { get; set; } = new List<ResearchSubLine>();
    }
}
namespace Domain.Entities
{
    public class Proposal : BaseEntity<int>
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }        public int IdResearchLine { get; set; }
        public int IdResearchSubLine { get; set; }
        public int IdStateStage { get; set; }

        // Navigation properties
        public virtual InscriptionModality InscriptionModality { get; set; } = null!;
        public virtual ResearchLine ResearchLine { get; set; } = null!;
        public virtual ResearchSubLine ResearchSubLine { get; set; } = null!;
        public virtual StateStage StateStage { get; set; } = null!;
    }
}
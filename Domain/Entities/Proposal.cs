namespace Domain.Entities
{
    public class Proposal : BaseEntity<int>
    {
        public new int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int IdResearchLine { get; set; }
        public int IdResearchSubLine { get; set; }
        public int IdStateProposal { get; set; }

        // Navigation properties
        public virtual InscriptionModality InscriptionModality { get; set; } = null!;
        public virtual ResearchLine ResearchLine { get; set; } = null!;
        public virtual ResearchSubLine ResearchSubLine { get; set; } = null!;
        public virtual StateProposal StateProposal { get; set; } = null!;
    }
}
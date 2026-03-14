using Domain.Entities;

namespace Domain.Entities
{
    public class ScientificArticle : BaseEntity<int>
    {
        public int IdStateStage { get; set; }
        public string? ArticleTitle { get; set; }
        public string? JournalName { get; set; }
        public string? ISSN { get; set; }
        public string? JournalCategory { get; set; }
        public DateTime? AcceptanceDate { get; set; }
        public string? Observations { get; set; }

        // Navigation properties
        public virtual InscriptionModality InscriptionModality { get; set; } = null!;
        public virtual StateStage StateStage { get; set; } = null!;
    }
}

using Domain.Events;

namespace Domain.Entities
{
    public class ScientificArticle : BaseEntity<int>
    {
        private int _idStateStage;
        public int IdStateStage
        {
            get => _idStateStage;
            set
            {
                if (_idStateStage != value && _idStateStage != 0)
                {
                    AddDomainEvent(new ScientificArticleStateChangedEvent(Id, 0, value, _idStateStage, IdUserUpdatedAt ?? IdUserCreatedAt ?? 1));
                }
                _idStateStage = value;
            }
        }
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

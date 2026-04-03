using Domain.Events;

namespace Domain.Entities
{
    public class Proposal : BaseEntity<int>
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Observation { get; set; }
        public string GeneralObjective { get; set; } = string.Empty;
        public List<string> SpecificObjectives { get; set; } = new List<string>();
        public int IdResearchLine { get; set; }
        public int IdResearchSubLine { get; set; }

        private int _idStateStage;
        public int IdStateStage
        {
            get => _idStateStage;
            set
            {
                if (_idStateStage != value && _idStateStage != 0)
                {
                    AddDomainEvent(new ProposalStateChangedEvent(Id, value, IdUserUpdatedAt ?? IdUserCreatedAt ?? 1));
                }
                _idStateStage = value;
            }
        }

        // Navigation properties
        public virtual InscriptionModality InscriptionModality { get; set; } = null!;
        public virtual ResearchLine ResearchLine { get; set; } = null!;
        public virtual ResearchSubLine ResearchSubLine { get; set; } = null!;
        public virtual StateStage StateStage { get; set; } = null!;
    }
}
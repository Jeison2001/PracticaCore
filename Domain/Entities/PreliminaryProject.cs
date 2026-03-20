using Domain.Events;

namespace Domain.Entities
{
    public class PreliminaryProject : BaseEntity<int>
    {
        private int _idStateStage;
        public int IdStateStage
        {
            get => _idStateStage;
            set
            {
                if (_idStateStage != value && _idStateStage != 0)
                {
                    AddDomainEvent(new PreliminaryProjectStateChangedEvent(Id, 0, value, IdUserUpdatedAt ?? IdUserCreatedAt ?? 1));
                }
                _idStateStage = value;
            }
        }

        public DateTime? ApprovalDate { get; set; }
        public string? Observations { get; set; }

        public virtual StateStage? StateStage { get; set; }
    }
}
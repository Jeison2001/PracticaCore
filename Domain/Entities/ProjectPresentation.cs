namespace Domain.Entities
{
    public class ProjectPresentation : BaseEntity<int>
    {
        private int _idStateStage;

        public int IdInscriptionModality { get; set; }

        public int IdStateStage
        {
            get => _idStateStage;
            set
            {
                if (_idStateStage != value && _idStateStage != 0)
                {
                    AddDomainEvent(new Domain.Events.ProjectPresentationStateChangedEvent(Id, _idStateStage, value, IdUserUpdatedAt ?? IdUserCreatedAt ?? 1));
                }
                _idStateStage = value;
            }
        }

        public DateTimeOffset? ScheduledDateTime { get; set; }
        public DateTimeOffset? ActualDateTime { get; set; }
        public string? LocationOrLink { get; set; }
        public string? PresentationModality { get; set; }
        public int AttemptNumber { get; set; } = 1;
        public string? Observations { get; set; }

        public virtual InscriptionModality InscriptionModality { get; set; } = null!;
        public virtual StateStage? StateStage { get; set; }
    }
}

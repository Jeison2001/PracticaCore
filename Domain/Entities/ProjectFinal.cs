namespace Domain.Entities
{    public class ProjectFinal : BaseEntity<int>
    {
        private int _idStateStage;
        public int IdStateStage
        {
            get => _idStateStage;
            set
            {
                if (_idStateStage != value && _idStateStage != 0)
                {
                    AddDomainEvent(new Domain.Events.ProjectFinalStateChangedEvent(Id, _idStateStage, value, IdUserUpdatedAt ?? IdUserCreatedAt ?? 1));
                }
                _idStateStage = value;
            }
        }
        public DateTimeOffset? ReportApprovalDate { get; set; }
        public DateTimeOffset? FinalPhaseApprovalDate { get; set; }
        public string? Observations { get; set; }

        public virtual InscriptionModality InscriptionModality { get; set; } = null!;
        public virtual StateStage? StateStage { get; set; }
    }
}
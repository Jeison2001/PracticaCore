using Domain.Events;

namespace Domain.Entities
{
    public class InscriptionModality : BaseEntity<int>
    {
        public int IdModality { get; set; }

        private int _idStateInscription;
        public int IdStateInscription
        {
            get => _idStateInscription;
            set
            {
                // Solo disparamos el evento si el estado cambia realmente,
                // omitimos si el estado_anterior era 0 (esto evita triggers en INSERTS vacíos o creación)
                if (_idStateInscription != 0 && _idStateInscription != value)
                {
                    AddDomainEvent(new InscriptionStateChangedEvent(this.Id, this.IdModality, value, this.IdUserUpdatedAt ?? 0));
                }
                _idStateInscription = value;
            }
        }
        public int IdAcademicPeriod { get; set; }
        public int? IdStageModality { get; set; }
        public DateTimeOffset? ApprovalDate { get; set; }
        public string? Observations { get; set; }

        // Propiedades de navegacion
        public virtual Modality? Modality { get; set; }
        public virtual StateInscription? StateInscription { get; set; }
        public virtual AcademicPeriod? AcademicPeriod { get; set; }
        public virtual StageModality? StageModality { get; set; }
        public virtual Proposal? Proposal { get; set; }
        public virtual AcademicPractice? AcademicPractice { get; set; }
        public virtual CoTerminal? CoTerminal { get; set; }
        public virtual Seminar? Seminar { get; set; }
        public virtual ScientificArticle? ScientificArticle { get; set; }
        public virtual AcademicAverage? AcademicAverage { get; set; }
        public virtual SaberPro? SaberPro { get; set; }
        public virtual ICollection<TeachingAssignment> TeachingAssignments { get; set; } = new List<TeachingAssignment>();
    }
}
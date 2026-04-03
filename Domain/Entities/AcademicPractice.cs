using Domain.Events;

namespace Domain.Entities
{
    public class AcademicPractice : BaseEntity<int>
    {
        private int _idStateStage;
        public int IdStateStage
        {
            get => _idStateStage;
            set
            {
                if (_idStateStage != value && _idStateStage != 0)
                {
                    AddDomainEvent(new AcademicPracticeStateChangedEvent(Id, value, _idStateStage, IdUserUpdatedAt ?? IdUserCreatedAt ?? 1));
                }
                _idStateStage = value;
            }
        }
        public string? Title { get; set; }
        public string? InstitutionName { get; set; }
        public string? InstitutionContact { get; set; }
        public DateTimeOffset? PracticeStartDate { get; set; }
        public DateTimeOffset? PracticeEndDate { get; set; }
        public bool IsEmprendimiento { get; set; } = false;
        public string? Observations { get; set; }
        
        // Fechas específicas por fase
        public DateTimeOffset? AvalApprovalDate { get; set; }
        public DateTimeOffset? PlanApprovalDate { get; set; }
        public DateTimeOffset? DevelopmentCompletionDate { get; set; }
        public DateTimeOffset? FinalReportApprovalDate { get; set; }
        public DateTimeOffset? FinalApprovalDate { get; set; }
        
        // Campos especificos de gestion
        public int? PracticeHours { get; set; } = 640; // Horas mi­nimas requeridas
        public string? EvaluatorObservations { get; set; }

        // Navigation properties
        public virtual InscriptionModality InscriptionModality { get; set; } = null!;
        public virtual StateStage StateStage { get; set; } = null!;
    }
}

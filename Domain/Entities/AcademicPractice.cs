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
                    AddDomainEvent(new AcademicPracticeStateChangedEvent(Id, 0, value, _idStateStage, IdUserUpdatedAt ?? IdUserCreatedAt ?? 1));
                }
                _idStateStage = value;
            }
        }
        public string? Title { get; set; }
        public string? InstitutionName { get; set; }
        public string? InstitutionContact { get; set; }
        public DateTime? PracticeStartDate { get; set; }
        public DateTime? PracticeEndDate { get; set; }
        public bool IsEmprendimiento { get; set; } = false;
        public string? Observations { get; set; }
        
        // Fechas específicas por fase
        public DateTime? AvalApprovalDate { get; set; }
        public DateTime? PlanApprovalDate { get; set; }
        public DateTime? DevelopmentCompletionDate { get; set; }
        public DateTime? FinalReportApprovalDate { get; set; }
        public DateTime? FinalApprovalDate { get; set; }
        
        // Campos especificos de gestion
        public int? PracticeHours { get; set; } = 640; // Horas mi­nimas requeridas
        public string? EvaluatorObservations { get; set; }

        // Navigation properties
        public virtual InscriptionModality InscriptionModality { get; set; } = null!;
        public virtual StateStage StateStage { get; set; } = null!;
    }
}

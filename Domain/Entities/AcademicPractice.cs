using System;

namespace Domain.Entities
{
    public class AcademicPractice : BaseEntity<int>
    {
        public int IdStateStage { get; set; }
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
        
        // Campos específicos de gestión
        public int? PracticeHours { get; set; } = 640; // Horas mínimas requeridas
        public string? EvaluatorObservations { get; set; }
        public new int? IdUserCreatedAt { get; set; }

        // Navigation properties
        public virtual InscriptionModality InscriptionModality { get; set; } = null!;
        public virtual StateStage StateStage { get; set; } = null!;
    }
}

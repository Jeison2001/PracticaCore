using System;

namespace Application.Shared.DTOs.AcademicPractice
{
    public class AcademicPracticeDto : BaseDto<int>
    {
        public new int Id { get; set; }
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
        public int? PracticeHours { get; set; }
        public string? EvaluatorObservations { get; set; }
        public new int? IdUserCreatedAt { get; set; }
    }
}

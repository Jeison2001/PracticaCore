using System;

namespace Application.Shared.DTOs.AcademicPractice
{
    public class AcademicPracticeDetailsDto
    {
        public int Id { get; set; }
        public string? InstitutionName { get; set; }
        public string? InstitutionContact { get; set; }
        public DateTime? PracticeStartDate { get; set; }
        public DateTime? PracticeEndDate { get; set; }
        public bool IsEmprendimiento { get; set; }
        public string? Observations { get; set; }
        public int? PracticeHours { get; set; }
        public string? EvaluatorObservations { get; set; }
        
        // Phase approval dates
        public DateTime? AvalApprovalDate { get; set; }
        public DateTime? PlanApprovalDate { get; set; }
        public DateTime? DevelopmentCompletionDate { get; set; }
        public DateTime? FinalReportApprovalDate { get; set; }
        public DateTime? FinalApprovalDate { get; set; }
        
        // Current state information
        public int IdStateStage { get; set; }
        public string StateStageCode { get; set; } = string.Empty;
        public string StateStageName { get; set; } = string.Empty;
        
        // Stage information
        public int? IdStageModality { get; set; }
        public string? StageModalityCode { get; set; }
        public string? StageModalityName { get; set; }
        public int? StageOrder { get; set; }
        
        // Base entity fields
        public int IdUserCreatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? IdUserUpdatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string OperationRegister { get; set; } = string.Empty;
        public bool StatusRegister { get; set; }
    }
}

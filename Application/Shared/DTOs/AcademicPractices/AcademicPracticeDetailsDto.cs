namespace Application.Shared.DTOs.AcademicPractices
{
    public record AcademicPracticeDetailsDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }                   // AÑADIDO
        public string? InstitutionName { get; set; }
        public string? InstitutionContact { get; set; }
        public DateTimeOffset? PracticeStartDate { get; set; }
        public DateTimeOffset? PracticeEndDate { get; set; }
        public bool IsEmprendimiento { get; set; }
        public string? Observations { get; set; }
        public int? PracticeHours { get; set; }
        public string? EvaluatorObservations { get; set; }
        
        // Phase approval dates
        public DateTimeOffset? AvalApprovalDate { get; set; }
        public DateTimeOffset? PlanApprovalDate { get; set; }
        public DateTimeOffset? DevelopmentCompletionDate { get; set; }
        public DateTimeOffset? FinalReportApprovalDate { get; set; }
        public DateTimeOffset? FinalApprovalDate { get; set; }
        
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
        public DateTimeOffset CreatedAt { get; set; }
        public int? IdUserUpdatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string OperationRegister { get; set; } = string.Empty;
        public bool StatusRegister { get; set; }
    }
}


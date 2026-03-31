namespace Application.Shared.DTOs.AcademicPractices
{
    public record AcademicPracticeDto : BaseDto<int>
    {
        public int IdStateStage { get; set; }
        public string? Title { get; set; }
        public string? InstitutionName { get; set; }
        public string? InstitutionContact { get; set; }
        public DateTimeOffset? PracticeStartDate { get; set; }
        public DateTimeOffset? PracticeEndDate { get; set; }
        public bool IsEmprendimiento { get; set; } = false;
        public string? Observations { get; set; }
        
        // Fechas especificas por fase
        public DateTimeOffset? AvalApprovalDate { get; set; }
        public DateTimeOffset? PlanApprovalDate { get; set; }
        public DateTimeOffset? DevelopmentCompletionDate { get; set; }
        public DateTimeOffset? FinalReportApprovalDate { get; set; }
        public DateTimeOffset? FinalApprovalDate { get; set; }
        
        // Campos especificos de gestion
        public int? PracticeHours { get; set; }
        public string? EvaluatorObservations { get; set; }
    }
}


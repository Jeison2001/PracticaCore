namespace Application.Shared.DTOs.AcademicPractices
{
    public record AcademicPracticeDto : BaseDto<int>
    {
        public int IdStateStage { get; set; }
        public string? Title { get; set; }
        public string? InstitutionName { get; set; }
        public string? InstitutionContact { get; set; }
        public DateTime? PracticeStartDate { get; set; }
        public DateTime? PracticeEndDate { get; set; }
        public bool IsEmprendimiento { get; set; } = false;
        public string? Observations { get; set; }
        
        // Fechas especificas por fase
        public DateTime? AvalApprovalDate { get; set; }
        public DateTime? PlanApprovalDate { get; set; }
        public DateTime? DevelopmentCompletionDate { get; set; }
        public DateTime? FinalReportApprovalDate { get; set; }
        public DateTime? FinalApprovalDate { get; set; }
        
        // Campos especificos de gestion
        public int? PracticeHours { get; set; }
        public string? EvaluatorObservations { get; set; }
    }
}

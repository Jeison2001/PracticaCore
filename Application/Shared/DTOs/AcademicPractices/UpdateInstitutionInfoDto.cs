namespace Application.Shared.DTOs.AcademicPractices
{
    public record UpdateInstitutionInfoDto : BaseDto<int>
    {
        public string? Title { get; set; }
        public string? InstitutionName { get; set; }
        public string? InstitutionContact { get; set; }
        public DateTimeOffset? PracticeStartDate { get; set; }
        public DateTimeOffset? PracticeEndDate { get; set; }
        public int? PracticeHours { get; set; }
        public bool IsEmprendimiento { get; set; }
        public string? Observations { get; set; }
        /// <summary>
        /// Estado de la etapa al que se desea mover tras radicar documentos iniciales (ej: PA_PEND_APROBACION_DOCUMENTOS).
        /// Opcional; si no se envía no se altera el IdStateStage.
        /// </summary>
        public int? NewStateStageId { get; set; }
    }
}


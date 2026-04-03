namespace Application.Shared.DTOs.ProjectFinals
{
    /// <summary>
    /// DTO para actualización parcial (PATCH) de ProjectFinal.
    /// Solo incluye campos que el frontend puede modificar: estado, fechas de aprobación y observaciones.
    /// </summary>
    public record ProjectFinalPatchDto
    {
        public int? IdStateStage { get; set; }
        public DateTimeOffset? ReportApprovalDate { get; set; }
        public DateTimeOffset? FinalPhaseApprovalDate { get; set; }
        public string? Observations { get; set; }
    }
}

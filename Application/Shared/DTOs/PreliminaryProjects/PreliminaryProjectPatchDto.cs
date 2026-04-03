namespace Application.Shared.DTOs.PreliminaryProjects
{
    /// <summary>
    /// DTO para actualización parcial (PATCH) de PreliminaryProject.
    /// Solo incluye campos que el frontend puede modificar: estado, fecha de aprobación y observaciones.
    /// </summary>
    public record PreliminaryProjectPatchDto
    {
        public int? IdStateStage { get; set; }
        public DateTimeOffset? ApprovalDate { get; set; }
        public string? Observations { get; set; }
    }
}

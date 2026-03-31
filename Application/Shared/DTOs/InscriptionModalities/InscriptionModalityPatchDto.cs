namespace Application.Shared.DTOs.InscriptionModalities
{
    /// <summary>
    /// DTO para actualización parcial (PATCH) de InscriptionModality.
    /// Solo incluye campos que el frontend puede modificar directamente.
    /// </summary>
    public record InscriptionModalityPatchDto
    {
        public int? IdStateInscription { get; set; }
        public DateTimeOffset? ApprovalDate { get; set; }
        public string? Observations { get; set; }
    }
}


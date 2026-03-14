namespace Application.Shared.DTOs.InscriptionWithStudents
{
    public record InscriptionModalityCreateDto
    {
        public int IdModality { get; set; }
        public int? IdAcademicPeriod { get; set; } // Opcional para auto-detección
        public string? Observations { get; set; }
    }
}

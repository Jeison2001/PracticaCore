namespace Application.Shared.DTOs.InscriptionWithStudents
{
    public class InscriptionWithStudentsCreateDto
    {
        public InscriptionModalityCreateDto InscriptionModality { get; set; } = new();
        public List<UserInscriptionModalityCreateDto> Students { get; set; } = new();
    }

    public class InscriptionModalityCreateDto
    {
        public int IdModality { get; set; }
        public int? IdAcademicPeriod { get; set; } // Opcional para auto-detección
        public string? Observations { get; set; }
    }

    public class UserInscriptionModalityCreateDto
    {
        public string Identification { get; set; } = string.Empty;
        public int IdIdentificationType { get; set; }
    }
}
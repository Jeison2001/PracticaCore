namespace Application.Shared.DTOs.InscriptionWithStudents
{
    public record InscriptionModalityUpdateDto
    {
        public int IdModality { get; set; }
        public int IdStateInscription { get; set; }
        public int IdAcademicPeriod { get; set; }
        public string Observations { get; set; } = string.Empty;
        public bool StatusRegister { get; set; }
    }
}

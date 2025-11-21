namespace Application.Shared.DTOs.InscriptionWithStudents
{
    public record UserInscriptionModalityCreateDto
    {
        public string Identification { get; set; } = string.Empty;
        public int IdIdentificationType { get; set; }
    }
}

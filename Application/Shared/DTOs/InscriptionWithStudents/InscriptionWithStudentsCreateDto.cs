namespace Application.Shared.DTOs.InscriptionWithStudents
{
    public record InscriptionWithStudentsCreateDto
    {
        public InscriptionModalityCreateDto InscriptionModality { get; set; } = new();
        public List<UserInscriptionModalityCreateDto> Students { get; set; } = new();
    }
}

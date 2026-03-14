namespace Application.Shared.DTOs.InscriptionWithStudents
{
    public record InscriptionWithStudentsUpdateDto
    {
        public InscriptionModalityUpdateDto InscriptionModality { get; set; } = null!;
        public List<UserInscriptionModalityUpdateDto> Students { get; set; } = new();
    }
}

namespace Application.Shared.DTOs.InscriptionWithStudents
{
    public record UserInscriptionModalityUpdateDto
    {
        public int Id { get; set; }
        public int IdUser { get; set; }
        public bool StatusRegister { get; set; }
    }
}

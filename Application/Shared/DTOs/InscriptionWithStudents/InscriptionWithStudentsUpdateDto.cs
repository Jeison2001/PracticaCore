namespace Application.Shared.DTOs.InscriptionWithStudents
{
    public class InscriptionWithStudentsUpdateDto
    {
        public InscriptionModalityUpdateDto InscriptionModality { get; set; } = null!;
        public List<UserInscriptionModalityUpdateDto> Students { get; set; } = new();
    }

    public class InscriptionModalityUpdateDto
    {
        public int IdModality { get; set; }
        public int IdStateInscription { get; set; }
        public int IdAcademicPeriod { get; set; }
        public string Observations { get; set; } = string.Empty;
        public bool StatusRegister { get; set; }
    }

    public class UserInscriptionModalityUpdateDto
    {
        public int Id { get; set; }
        public int IdUser { get; set; }
        public bool StatusRegister { get; set; }
    }
}
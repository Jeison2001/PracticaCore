namespace Application.Shared.DTOs.RegisterModalityWithStudents
{
    public class RegisterModalityWithStudentsCreateDto
    {
        public InscriptionModalityCreateDto InscriptionModality { get; set; }
        public List<UserInscriptionModalityCreateDto> Students { get; set; }
    }

    public class InscriptionModalityCreateDto
    {
        public int IdModality { get; set; }
        public int IdStateInscription { get; set; }
        public int IdAcademicPeriod { get; set; }
        public string? Observations { get; set; }
    }

    public class UserInscriptionModalityCreateDto
    {
        public int IdUser { get; set; }
        public string Identification { get; set; }
        public int IdIdentificationType { get; set; }
    }
}
using Application.Shared.DTOs.InscriptionModality;
using Application.Shared.DTOs.UserInscriptionModality;

namespace Application.Shared.DTOs.RegisterModalityWithStudents
{
    public class RegisterModalityWithStudentsResponseDto
    {
        public InscriptionModalityDto InscriptionModality { get; set; }
        public string AcademicPeriodCode { get; set; }
        public string ModalityName { get; set; }
        public string StateInscriptionName { get; set; }
        public List<UserInscriptionModalityDto> Students { get; set; }
    }
}
using Application.Shared.DTOs.InscriptionModality;
using Application.Shared.DTOs.UserInscriptionModality;

namespace Application.Shared.DTOs.InscriptionWithStudents
{
    public class InscriptionWithStudentsDto
    {
        public InscriptionModalityDto InscriptionModality { get; set; } = new InscriptionModalityDto();
        public List<UserInscriptionModalityDto> Students { get; set; } = new List<UserInscriptionModalityDto>();
    }
}
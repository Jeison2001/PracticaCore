using Application.Shared.DTOs.InscriptionModalities;
using Application.Shared.DTOs.UserInscriptionModalities;

namespace Application.Shared.DTOs.InscriptionWithStudents
{
    public record InscriptionWithStudentsDto
    {
        public InscriptionModalityDto InscriptionModality { get; set; } = new InscriptionModalityDto();
        public List<UserInscriptionModalityDto> Students { get; set; } = new List<UserInscriptionModalityDto>();
    }
}

using Application.Shared.DTOs.InscriptionModality;
using Application.Shared.DTOs.UserInscriptionModality;

namespace Application.Shared.DTOs.InscriptionWithStudents
{
    public class InscriptionWithStudentsResponseDto
    {
        public InscriptionModalityDto InscriptionModality { get; set; }
        public string AcademicPeriodCode { get; set; }
        public string ModalityName { get; set; }
        public string StateInscriptionName { get; set; }
        public string? StageModalityName { get; set; }
        public int? StageOrder { get; set; }
        public List<UserInscriptionModalityDto> Students { get; set; }
        public string? StateInscriptionCode { get; set; } // Código del estado de la inscripción
    }
}
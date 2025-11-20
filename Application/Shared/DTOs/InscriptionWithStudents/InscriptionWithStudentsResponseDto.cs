using Application.Shared.DTOs.InscriptionModalities;
using Application.Shared.DTOs.UserInscriptionModalities;

namespace Application.Shared.DTOs.InscriptionWithStudents
{
    public class InscriptionWithStudentsResponseDto
    {
        public InscriptionModalityDto InscriptionModality { get; set; } = null!;
        public string AcademicPeriodCode { get; set; } = string.Empty;
        public string ModalityName { get; set; } = string.Empty;
        public string StateInscriptionName { get; set; } = string.Empty;
        public string? StageModalityName { get; set; }
        public int? StageOrder { get; set; }
        public List<UserInscriptionModalityDto> Students { get; set; } = new();
        public string? StateInscriptionCode { get; set; } // Código del estado de la inscripción
    }
}
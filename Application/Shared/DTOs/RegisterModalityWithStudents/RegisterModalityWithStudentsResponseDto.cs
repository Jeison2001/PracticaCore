using Application.Shared.DTOs.RegisterModality;
using Application.Shared.DTOs.RegisterModalityStudent;

namespace Application.Shared.DTOs.RegisterModalityWithStudents
{
    public class RegisterModalityWithStudentsResponseDto
    {
        public RegisterModalityDto RegisterModality { get; set; }
        public string AcademicPeriodCode { get; set; }
        public string ModalityName { get; set; }
        public string RegisterModalityStateName { get; set; }
        public List<RegisterModalityStudentDto> Students { get; set; }
    }
}
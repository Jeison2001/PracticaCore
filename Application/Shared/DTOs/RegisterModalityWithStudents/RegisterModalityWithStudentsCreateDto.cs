using Application.Shared.DTOs.RegisterModality;
using Application.Shared.DTOs.RegisterModalityStudent;
using System.Collections.Generic;

namespace Application.Shared.DTOs.RegisterModalityWithStudents
{
    public class RegisterModalityWithStudentsCreateDto
    {
        public RegisterModalityCreateDto RegisterModality { get; set; }
        public List<RegisterModalityStudentCreateDto> Students { get; set; }
    }

    public class RegisterModalityCreateDto
    {
        public int IdModality { get; set; }
        public int IdStateInscription { get; set; }
        public int IdAcademicPeriod { get; set; }
        public string? Observations { get; set; }
    }

    public class RegisterModalityStudentCreateDto
    {
        public int IdUser { get; set; }
        public string Identification { get; set; }
        public int IdIdentificationType { get; set; }
    }
}
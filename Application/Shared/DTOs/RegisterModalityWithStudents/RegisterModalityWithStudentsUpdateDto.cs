using Application.Shared.DTOs.InscriptionModality;
using Application.Shared.DTOs.UserInscriptionModality;
using System.Collections.Generic;

namespace Application.Shared.DTOs.RegisterModalityWithStudents
{
    public class RegisterModalityWithStudentsUpdateDto
    {
        public RegisterModalityUpdateDto InscriptionModality { get; set; }
        public List<RegisterModalityStudentUpdateDto> Students { get; set; }
    }

    public class RegisterModalityUpdateDto
    {
        public int IdModality { get; set; }
        public int IdStateInscription { get; set; }
        public int IdAcademicPeriod { get; set; }
        public string Observations { get; set; }
        public bool StatusRegister { get; set; } // Added StatusRegister
    }

    public class RegisterModalityStudentUpdateDto
    {
        public int Id { get; set; }
        public int IdUser { get; set; }
        public bool StatusRegister { get; set; } // Added StatusRegister
    }
}
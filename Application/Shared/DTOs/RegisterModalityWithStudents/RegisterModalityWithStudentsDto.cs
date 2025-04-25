using System;
using System.Collections.Generic;
using Application.Shared.DTOs.RegisterModality;
using Application.Shared.DTOs.RegisterModalityStudent;

namespace Application.Shared.DTOs.RegisterModalityWithStudents
{
    public class RegisterModalityWithStudentsDto
    {
        public RegisterModalityDto RegisterModality { get; set; } = new RegisterModalityDto();
        public List<RegisterModalityStudentDto> Students { get; set; } = new List<RegisterModalityStudentDto>();
    }
}
using System;
using System.Collections.Generic;
using Application.Shared.DTOs.InscriptionModality;
using Application.Shared.DTOs.UserInscriptionModality;

namespace Application.Shared.DTOs.RegisterModalityWithStudents
{
    public class RegisterModalityWithStudentsDto
    {
        public InscriptionModalityDto InscriptionModality { get; set; } = new InscriptionModalityDto();
        public List<UserInscriptionModalityDto> Students { get; set; } = new List<UserInscriptionModalityDto>();
    }
}
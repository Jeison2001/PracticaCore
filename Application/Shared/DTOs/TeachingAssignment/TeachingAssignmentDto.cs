using Application.Shared.DTOs;
using System;

namespace Application.Shared.DTOs.TeachingAssignment
{
    public class TeachingAssignmentDto : BaseDto<int>
    {
        public int IdInscriptionModality { get; set; }
        public int IdTeacher { get; set; }
        public int IdTypeTeachingAssignment { get; set; }
        public DateTime? RevocationDate { get; set; }
        
        // Optional properties for related data
        public string? TeacherName { get; set; }
        public string? TypeTeachingAssignmentName { get; set; }
        public string? InscriptionModalityName { get; set; }
    }
}
using Application.Shared.DTOs.AcademicPractice;
using Application.Shared.DTOs.TeachingAssignment;
using Application.Shared.DTOs.User;
using Application.Shared.DTOs;

namespace Application.Shared.DTOs.AcademicPractice
{
    public class AcademicPracticeWithDetailsResponseDto
    {
        public AcademicPracticeDetailsDto? AcademicPractice { get; set; }
        
        // Inscription details
        public int InscriptionModalityId { get; set; }
        public string ModalityName { get; set; } = string.Empty;
        public string StateInscriptionName { get; set; } = string.Empty;
        public string AcademicPeriodCode { get; set; } = string.Empty;
        public DateTime? InscriptionApprovalDate { get; set; }
        public string? InscriptionObservations { get; set; }
        
        // Students information - usando UserDto existente
        public List<UserDto> Students { get; set; } = new List<UserDto>();
        
        // Teachers information - usando TeachingAssignmentTeacherDto existente
        public List<TeachingAssignmentTeacherDto> Teachers { get; set; } = new List<TeachingAssignmentTeacherDto>();
        
        // Documents by phase - usando DocumentDto existente
        public List<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
        
        // Phase progress tracking
        public AcademicPracticePhaseProgressDto PhaseProgress { get; set; } = new AcademicPracticePhaseProgressDto();
    }
}

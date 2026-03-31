using Application.Shared.DTOs.TeachingAssignments;
using Application.Shared.DTOs.Users;
using Application.Shared.DTOs.Documents;

namespace Application.Shared.DTOs.AcademicPractices
{
    public record AcademicPracticeWithDetailsResponseDto
    {
        public AcademicPracticeDetailsDto? AcademicPractice { get; set; }
        
        // Inscription details
        public int InscriptionModalityId { get; set; }
        public string ModalityName { get; set; } = string.Empty;
        public string StateInscriptionName { get; set; } = string.Empty;
        public string AcademicPeriodCode { get; set; } = string.Empty;
        public DateTimeOffset? InscriptionApprovalDate { get; set; }
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


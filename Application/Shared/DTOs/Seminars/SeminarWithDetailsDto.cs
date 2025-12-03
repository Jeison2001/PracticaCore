using Application.Shared.DTOs.Documents;
using Application.Shared.DTOs.TeachingAssignments;
using Application.Shared.DTOs.Users;

namespace Application.Shared.DTOs.Seminars
{
    public record SeminarWithDetailsDto
    {
        public SeminarDto? Seminar { get; set; }

        // Inscription details
        public int InscriptionModalityId { get; set; }
        public string ModalityName { get; set; } = string.Empty;
        public string StateInscriptionName { get; set; } = string.Empty;
        public string AcademicPeriodCode { get; set; } = string.Empty;
        public DateTime? InscriptionApprovalDate { get; set; }
        public string? InscriptionObservations { get; set; }

        // Students information
        public List<UserDto> Students { get; set; } = new();

        // Teachers information
        public List<TeachingAssignmentTeacherDto> Teachers { get; set; } = new();

        // Documents
        public List<DocumentDto> Documents { get; set; } = new();
    }
}

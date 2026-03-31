using Application.Shared.DTOs.Documents;
using Application.Shared.DTOs.TeachingAssignments;
using Application.Shared.DTOs.Users;

namespace Application.Shared.DTOs.SaberPros
{
    public record SaberProWithDetailsDto
    {
        public SaberProDto? SaberPro { get; set; }

        // Inscription details
        public int InscriptionModalityId { get; set; }
        public string ModalityName { get; set; } = string.Empty;
        public string StateInscriptionName { get; set; } = string.Empty;
        public string AcademicPeriodCode { get; set; } = string.Empty;
        public DateTimeOffset? InscriptionApprovalDate { get; set; }
        public string? InscriptionObservations { get; set; }

        // Students information
        public List<UserDto> Students { get; set; } = new();

        // Teachers information
        public List<TeachingAssignmentTeacherDto> Teachers { get; set; } = new();

        // Documents
        public List<DocumentDto> Documents { get; set; } = new();
    }
}


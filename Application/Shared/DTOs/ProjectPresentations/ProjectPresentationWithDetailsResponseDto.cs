using Application.Shared.DTOs.ProjectFinals;
using Application.Shared.DTOs.Proposals;
using Application.Shared.DTOs.Teachers;
using Application.Shared.DTOs.UserInscriptionModalities;

namespace Application.Shared.DTOs.ProjectPresentations
{
    public record ProjectPresentationWithDetailsResponseDto
    {
        public required ProjectPresentationDetailsDto ProjectPresentation { get; set; }
        public ProjectFinalDetailsDto? ProjectFinal { get; set; }
        public required ProposalWithDetailsResponseDto Proposal { get; set; }
        public List<UserInscriptionModalityDto>? Students { get; set; }
        public List<AssignedTeacherDto>? Teachers { get; set; }
    }
}

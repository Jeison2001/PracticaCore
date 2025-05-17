using Application.Shared.DTOs.Proposal;

namespace Application.Shared.DTOs.ProjectFinal
{
    public class ProjectFinalWithDetailsResponseDto
    {
        public required ProjectFinalDetailsDto ProjectFinal { get; set; }
        public required ProposalWithDetailsResponseDto Proposal { get; set; }
        // Puedes agregar aquí más propiedades relacionadas si es necesario
    }
}

using Application.Shared.DTOs.Proposals;

namespace Application.Shared.DTOs.PreliminaryProjects
{
    public record PreliminaryProjectWithDetailsResponseDto
    {
        public required PreliminaryProjectDetailsDto PreliminaryProject { get; set; }
        public required ProposalWithDetailsResponseDto Proposal { get; set; }
        // Puedes agregar aquí más propiedades relacionadas si es necesario
    }
}

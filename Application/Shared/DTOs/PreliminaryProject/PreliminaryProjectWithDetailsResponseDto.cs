using Application.Shared.DTOs.Proposal;

namespace Application.Shared.DTOs.PreliminaryProject
{
    public class PreliminaryProjectWithDetailsResponseDto
    {
        public required PreliminaryProjectDetailsDto PreliminaryProject { get; set; }
        public required ProposalWithDetailsResponseDto Proposal { get; set; }
        // Puedes agregar aquí más propiedades relacionadas si es necesario
    }
}

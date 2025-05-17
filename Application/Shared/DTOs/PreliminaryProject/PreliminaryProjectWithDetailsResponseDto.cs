using Application.Shared.DTOs.Proposal;

namespace Application.Shared.DTOs.PreliminaryProject
{
    public class PreliminaryProjectWithDetailsResponseDto
    {
        public PreliminaryProjectDto PreliminaryProject { get; set; }
        public ProposalDto Proposal { get; set; }
        // Puedes agregar aquí más propiedades relacionadas si es necesario
    }
}

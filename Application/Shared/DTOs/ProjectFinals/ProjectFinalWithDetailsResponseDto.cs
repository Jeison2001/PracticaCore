using Application.Shared.DTOs.Proposals;

namespace Application.Shared.DTOs.ProjectFinals
{
    public record ProjectFinalWithDetailsResponseDto
    {
        public required ProjectFinalDetailsDto ProjectFinal { get; set; }
        public required ProposalWithDetailsResponseDto Proposal { get; set; }
        // Puedes agregar aquí más propiedades relacionadas si es necesario
    }
}

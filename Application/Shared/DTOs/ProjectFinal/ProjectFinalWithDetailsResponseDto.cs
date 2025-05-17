using Application.Shared.DTOs.Proposal;

namespace Application.Shared.DTOs.ProjectFinal
{
    public class ProjectFinalWithDetailsResponseDto
    {
        public ProjectFinalDto ProjectFinal { get; set; }
        public ProposalDto Proposal { get; set; }
        // Puedes agregar aquí más propiedades relacionadas si es necesario
    }
}

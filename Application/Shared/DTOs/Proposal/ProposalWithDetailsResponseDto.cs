using Application.Shared.DTOs.UserInscriptionModality;
using Application.Shared.DTOs.Proposal;

namespace Application.Shared.DTOs.Proposal
{
    public class ProposalWithDetailsResponseDto
    {
        public required ProposalDto Proposal { get; set; }
        public required string StateStageName { get; set; }
        public required string ResearchLineName { get; set; }
        public required string ResearchSubLineName { get; set; }
        public required List<UserInscriptionModalityDto> Students { get; set; }
    }
}
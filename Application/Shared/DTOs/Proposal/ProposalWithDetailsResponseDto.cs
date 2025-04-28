using Application.Shared.DTOs.InscriptionModality;
using Application.Shared.DTOs.UserInscriptionModality;
using Application.Shared.DTOs.StateProposal;
using Application.Shared.DTOs.Proposal;

namespace Application.Shared.DTOs.Proposal
{
    public class ProposalWithDetailsResponseDto
    {
        public ProposalDto Proposal { get; set; }
        public InscriptionModalityDto InscriptionModality { get; set; }
        public string StateProposalName { get; set; }
        public string ResearchLineName { get; set; }
        public string ResearchSubLineName { get; set; }
        public List<UserInscriptionModalityDto> Students { get; set; }
    }
}
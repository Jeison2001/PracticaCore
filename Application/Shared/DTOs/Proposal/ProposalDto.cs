using Application.Shared.DTOs;

namespace Application.Shared.DTOs.Proposal
{
    public class ProposalDto : BaseDto<int>
    {
        public new int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int IdResearchLine { get; set; }
        public int IdResearchSubLine { get; set; }
        public int IdStateProposal { get; set; }
    }
}
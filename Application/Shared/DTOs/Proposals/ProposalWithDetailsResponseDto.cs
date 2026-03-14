using Application.Shared.DTOs.UserInscriptionModalities;

namespace Application.Shared.DTOs.Proposals
{
    public record ProposalWithDetailsResponseDto
    {
        public required ProposalDto Proposal { get; set; }
        public required string StateStageName { get; set; }
        public required string ResearchLineName { get; set; }
        public required string ResearchSubLineName { get; set; }
        public required List<UserInscriptionModalityDto> Students { get; set; }
        public string? StateStageCode { get; set; } // Código del estado de la fase
    }
}

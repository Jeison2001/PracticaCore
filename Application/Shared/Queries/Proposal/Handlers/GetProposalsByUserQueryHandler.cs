using Application.Shared.DTOs.Proposal;
using Application.Shared.DTOs.UserInscriptionModality;
using AutoMapper;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries.Proposal.Handlers
{
    public class GetProposalsByUserQueryHandler : IRequestHandler<GetProposalsByUserQuery, List<ProposalWithDetailsResponseDto>>
    {
        private readonly IProposalRepository _proposalRepository;
        private readonly IMapper _mapper;

        public GetProposalsByUserQueryHandler(
            IProposalRepository proposalRepository,
            IMapper mapper)
        {
            _proposalRepository = proposalRepository;
            _mapper = mapper;
        }

        public async Task<List<ProposalWithDetailsResponseDto>> Handle(GetProposalsByUserQuery request, CancellationToken cancellationToken)
        {
            var entities = await _proposalRepository.GetProposalsByUserWithDetailsAsync(
                request.UserId,
                request.Status,
                cancellationToken);            return entities.Select(e => new ProposalWithDetailsResponseDto
            {
                Proposal = _mapper.Map<ProposalDto>(e.Proposal),                
                StateStageName = e.Proposal.StateStage?.Name ?? string.Empty,
                ResearchLineName = e.Proposal.ResearchLine?.Name ?? string.Empty,
                ResearchSubLineName = e.Proposal.ResearchSubLine?.Name ?? string.Empty,
                Students = e.UserInscriptionModalities.Select(uim => new UserInscriptionModalityDto
                {
                    IdInscriptionModality = uim.IdInscriptionModality,
                    IdUser = uim.IdUser,
                    UserName = uim.User?.FirstName + " " + uim.User?.LastName,
                    Email = uim.User?.Email ?? string.Empty
                }).ToList()
            }).ToList();
        }
    }
}
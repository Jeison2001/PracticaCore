using Application.Shared.DTOs.Proposals;
using MediatR;

namespace Application.Shared.Queries.Proposals
{
    public record GetProposalWithDetailsQuery(int Id) : IRequest<ProposalWithDetailsResponseDto>;
}

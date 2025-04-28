using Application.Shared.DTOs.Proposal;
using MediatR;

namespace Application.Shared.Queries.Proposal
{
    public record GetProposalsByUserQuery(int UserId) : IRequest<List<ProposalWithDetailsResponseDto>>;
}
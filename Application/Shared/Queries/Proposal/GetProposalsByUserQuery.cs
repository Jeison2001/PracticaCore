using Application.Shared.DTOs.Proposal;
using MediatR;

namespace Application.Shared.Queries.Proposal
{
    public record GetProposalsByUserQuery(int UserId, bool? Status) : IRequest<List<ProposalWithDetailsResponseDto>>;
}
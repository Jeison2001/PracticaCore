using Application.Shared.DTOs.Proposals;
using MediatR;

namespace Application.Shared.Queries.Proposals
{
    public record GetProposalsByUserQuery(int UserId, bool? Status) : IRequest<List<ProposalWithDetailsResponseDto>>;
}

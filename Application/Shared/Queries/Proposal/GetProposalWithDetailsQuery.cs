using Application.Shared.DTOs.Proposal;
using MediatR;

namespace Application.Shared.Queries.Proposal
{
    public record GetProposalWithDetailsQuery(int Id) : IRequest<ProposalWithDetailsResponseDto>;
}
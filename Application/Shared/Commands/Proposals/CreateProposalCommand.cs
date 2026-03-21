using Application.Shared.DTOs.Proposals;
using MediatR;

namespace Application.Shared.Commands.Proposals
{
    public record CreateProposalCommand(CreateProposalDto Dto) : IRequest<ProposalDto>;
}

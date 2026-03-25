using Application.Shared.DTOs.Proposals;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.Proposals
{
    public record CreateProposalCommand(CreateProposalDto Dto, CurrentUserInfo CurrentUser) : IRequest<ProposalDto>;
}

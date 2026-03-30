using Application.Shared.DTOs.Proposals;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.Proposals
{
    public record PatchProposalCommand(
        int Id,
        ProposalPatchDto Dto,
        CurrentUserInfo CurrentUser
    ) : IRequest<ProposalDto>;
}

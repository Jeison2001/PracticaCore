using Application.Shared.DTOs.ProjectFinals;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.ProjectFinals
{
    public record PatchProjectFinalCommand(
        int Id,
        ProjectFinalPatchDto Dto,
        CurrentUserInfo CurrentUser
    ) : IRequest<ProjectFinalDto>;
}

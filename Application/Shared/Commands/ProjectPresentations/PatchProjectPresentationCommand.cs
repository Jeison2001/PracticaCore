using Application.Shared.DTOs.ProjectPresentations;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.ProjectPresentations
{
    public record PatchProjectPresentationCommand(
        int Id,
        ProjectPresentationPatchDto Dto,
        CurrentUserInfo CurrentUser
    ) : IRequest<ProjectPresentationDto>;
}

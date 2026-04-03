using Application.Shared.DTOs.PreliminaryProjects;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.PreliminaryProjects
{
    public record PatchPreliminaryProjectCommand(
        int Id,
        PreliminaryProjectPatchDto Dto,
        CurrentUserInfo CurrentUser
    ) : IRequest<PreliminaryProjectDto>;
}

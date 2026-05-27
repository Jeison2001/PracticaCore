using Application.Shared.DTOs.Seminars;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.Seminars
{
    public record PatchSeminarCommand(
        int Id,
        SeminarPatchDto Dto,
        CurrentUserInfo CurrentUser
    ) : IRequest<SeminarDto>;
}

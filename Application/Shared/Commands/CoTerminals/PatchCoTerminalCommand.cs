using Application.Shared.DTOs.CoTerminals;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.CoTerminals
{
    public record PatchCoTerminalCommand(
        int Id,
        CoTerminalPatchDto Dto,
        CurrentUserInfo CurrentUser
    ) : IRequest<CoTerminalDto>;
}

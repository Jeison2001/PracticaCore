using Application.Shared.DTOs.SaberPros;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.SaberPros
{
    public record PatchSaberProCommand(
        int Id,
        SaberProPatchDto Dto,
        CurrentUserInfo CurrentUser
    ) : IRequest<SaberProDto>;
}

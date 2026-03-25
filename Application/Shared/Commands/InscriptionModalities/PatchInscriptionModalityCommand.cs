using Application.Shared.DTOs.InscriptionModalities;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.InscriptionModalities
{
    public record PatchInscriptionModalityCommand(
        int Id,
        InscriptionModalityPatchDto Dto,
        CurrentUserInfo CurrentUser
    ) : IRequest<InscriptionModalityDto>;
}

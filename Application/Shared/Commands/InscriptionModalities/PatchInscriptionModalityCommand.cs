using Application.Shared.DTOs.InscriptionModalities;
using MediatR;

namespace Application.Shared.Commands.InscriptionModalities
{
    public record PatchInscriptionModalityCommand(
        int Id,
        InscriptionModalityPatchDto Dto,
        int UserId
    ) : IRequest<InscriptionModalityDto>;
}

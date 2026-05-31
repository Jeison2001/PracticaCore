using Application.Shared.DTOs.AcademicAverages;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.AcademicAverages
{
    public record PatchAcademicAverageCommand(
        int Id,
        AcademicAveragePatchDto Dto,
        CurrentUserInfo CurrentUser
    ) : IRequest<AcademicAverageDto>;
}

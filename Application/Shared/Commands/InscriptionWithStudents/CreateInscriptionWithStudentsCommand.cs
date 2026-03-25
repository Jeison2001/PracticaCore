using Application.Shared.DTOs.InscriptionWithStudents;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.InscriptionWithStudents
{
    public record CreateInscriptionWithStudentsCommand(InscriptionWithStudentsCreateDto Dto, CurrentUserInfo? CurrentUser = null) : IRequest<InscriptionWithStudentsDto>;
}

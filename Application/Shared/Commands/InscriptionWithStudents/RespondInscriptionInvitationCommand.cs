using Application.Shared.DTOs.InscriptionWithStudents;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.InscriptionWithStudents
{
    public record RespondInscriptionInvitationCommand(int InscriptionModalityId, bool Accept, CurrentUserInfo CurrentUser)
        : IRequest<InscriptionWithStudentsResponseDto>;
}

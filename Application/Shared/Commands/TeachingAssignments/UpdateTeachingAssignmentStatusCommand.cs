using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.TeachingAssignments
{
    public record UpdateTeachingAssignmentStatusCommand(
        int Id,
        bool StatusRegister,
        CurrentUserInfo CurrentUser,
        string? OperationRegister = null) : IRequest<bool>;
}

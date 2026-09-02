using Application.Shared.DTOs.TeacherEnabledCargos;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.TeacherEnabledCargos
{
    public record AssignTeacherEnabledCargosCommand : IRequest<List<TeacherEnabledCargoDto>>
    {
        public int UserId { get; init; }
        public List<int> TypeTeachingAssignmentIds { get; init; } = new();
        public CurrentUserInfo CurrentUser { get; init; } = null!;
    }
}

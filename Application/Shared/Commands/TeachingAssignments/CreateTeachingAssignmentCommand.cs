using Application.Shared.DTOs.TeachingAssignments;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.TeachingAssignments
{
    public record CreateTeachingAssignmentCommand : IRequest<TeachingAssignmentDto>
    {
        public TeachingAssignmentDto Dto { get; }
        public CurrentUserInfo CurrentUser { get; }

        public CreateTeachingAssignmentCommand(TeachingAssignmentDto dto, CurrentUserInfo currentUser)
        {
            Dto = dto;
            CurrentUser = currentUser;
        }
    }
}

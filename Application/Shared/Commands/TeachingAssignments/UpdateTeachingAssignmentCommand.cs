using Application.Shared.DTOs.TeachingAssignments;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.TeachingAssignments
{
    public record UpdateTeachingAssignmentCommand : IRequest<TeachingAssignmentDto>
    {
        public int Id { get; }
        public TeachingAssignmentDto Dto { get; }
        public CurrentUserInfo CurrentUser { get; }

        public UpdateTeachingAssignmentCommand(int id, TeachingAssignmentDto dto, CurrentUserInfo currentUser)
        {
            Id = id;
            Dto = dto;
            CurrentUser = currentUser;
        }
    }
}

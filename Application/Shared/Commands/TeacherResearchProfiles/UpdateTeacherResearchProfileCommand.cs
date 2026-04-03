using Application.Shared.DTOs.TeacherResearchProfiles;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.TeacherResearchProfiles
{
    public record UpdateTeacherResearchProfileCommand : IRequest<TeacherResearchProfileDto>
    {
        public int Id { get; }
        public TeacherResearchProfileDto Dto { get; }
        public CurrentUserInfo CurrentUser { get; }
        public UpdateTeacherResearchProfileCommand(int id, TeacherResearchProfileDto dto, CurrentUserInfo currentUser)
        {
            Id = id;
            Dto = dto;
            CurrentUser = currentUser;
        }
    }
}

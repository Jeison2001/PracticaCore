using Application.Shared.DTOs.TeacherResearchProfiles;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.TeacherResearchProfiles
{
    public record CreateTeacherResearchProfileCommand : IRequest<TeacherResearchProfileDto>
    {
        public TeacherResearchProfileDto Dto { get; }
        public CurrentUserInfo CurrentUser { get; }
        public CreateTeacherResearchProfileCommand(TeacherResearchProfileDto dto, CurrentUserInfo currentUser)
        {
            Dto = dto;
            CurrentUser = currentUser;
        }
    }
}

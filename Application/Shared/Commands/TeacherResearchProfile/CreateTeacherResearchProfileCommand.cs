using Application.Shared.DTOs.TeacherResearchProfile;
using MediatR;

namespace Application.Shared.Commands.TeacherResearchProfile
{
    public class CreateTeacherResearchProfileCommand : IRequest<TeacherResearchProfileDto>
    {
        public TeacherResearchProfileDto Dto { get; }
        public CreateTeacherResearchProfileCommand(TeacherResearchProfileDto dto)
        {
            Dto = dto;
        }
    }
}

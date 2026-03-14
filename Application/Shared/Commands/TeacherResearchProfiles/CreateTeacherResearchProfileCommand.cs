using Application.Shared.DTOs.TeacherResearchProfiles;
using MediatR;

namespace Application.Shared.Commands.TeacherResearchProfiles
{
    public record CreateTeacherResearchProfileCommand : IRequest<TeacherResearchProfileDto>
    {
        public TeacherResearchProfileDto Dto { get; }
        public CreateTeacherResearchProfileCommand(TeacherResearchProfileDto dto)
        {
            Dto = dto;
        }
    }
}

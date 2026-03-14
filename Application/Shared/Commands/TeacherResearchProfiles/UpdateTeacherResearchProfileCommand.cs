using Application.Shared.DTOs.TeacherResearchProfiles;
using MediatR;

namespace Application.Shared.Commands.TeacherResearchProfiles
{
    public record UpdateTeacherResearchProfileCommand : IRequest<TeacherResearchProfileDto>
    {
        public int Id { get; }
        public TeacherResearchProfileDto Dto { get; }
        public UpdateTeacherResearchProfileCommand(int id, TeacherResearchProfileDto dto)
        {
            Id = id;
            Dto = dto;
        }
    }
}

using Application.Shared.DTOs.TeacherResearchProfiles;
using MediatR;

namespace Application.Shared.Commands.TeacherResearchProfiles
{
    public class UpdateTeacherResearchProfileCommand : IRequest<TeacherResearchProfileDto>
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

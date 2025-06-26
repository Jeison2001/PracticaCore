using Application.Shared.DTOs.TeacherResearchProfile;
using MediatR;

namespace Application.Shared.Commands.TeacherResearchProfile
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

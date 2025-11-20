using Application.Shared.DTOs.TeachingAssignments;
using MediatR;

namespace Application.Shared.Commands.TeachingAssignments
{
    public class CreateTeachingAssignmentCommand : IRequest<TeachingAssignmentDto>
    {
        public TeachingAssignmentDto Dto { get; }
        public CreateTeachingAssignmentCommand(TeachingAssignmentDto dto)
        {
            Dto = dto;
        }
    }
}

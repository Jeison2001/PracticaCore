using Application.Shared.DTOs.TeachingAssignment;
using MediatR;

namespace Application.Shared.Commands.TeachingAssignment
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

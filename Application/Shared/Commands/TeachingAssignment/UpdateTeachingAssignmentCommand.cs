using Application.Shared.DTOs.TeachingAssignment;
using MediatR;

namespace Application.Shared.Commands.TeachingAssignment
{
    public class UpdateTeachingAssignmentCommand : IRequest<TeachingAssignmentDto>
    {
        public int Id { get; }
        public TeachingAssignmentDto Dto { get; }
        public UpdateTeachingAssignmentCommand(int id, TeachingAssignmentDto dto)
        {
            Id = id;
            Dto = dto;
        }
    }
}

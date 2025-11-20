using Application.Shared.DTOs.TeachingAssignments;
using MediatR;

namespace Application.Shared.Commands.TeachingAssignments
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

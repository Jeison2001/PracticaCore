using Application.Shared.DTOs.TeachingAssignments;
using MediatR;

namespace Application.Shared.Queries.TeachingAssignments
{
    public record GetTeachingAssignmentsByTeacherQuery : IRequest<List<TeacherCargoGroupDto>>
    {
        public int TeacherId { get; init; }
        public int? CargoId { get; init; }
        public bool IncludeRevoked { get; init; }

        public GetTeachingAssignmentsByTeacherQuery(int teacherId, int? cargoId = null, bool includeRevoked = false)
        {
            TeacherId = teacherId;
            CargoId = cargoId;
            IncludeRevoked = includeRevoked;
        }
    }
}

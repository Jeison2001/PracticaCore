using Application.Shared.DTOs.TeachingAssignments;
using MediatR;

namespace Application.Shared.Queries.TeachingAssignments
{
    public record GetTeachingAssignmentsByTeacherAndCargoQuery : IRequest<List<TeacherAssignedProjectDto>>
    {
        public int TeacherId { get; init; }
        public int CargoId { get; init; }
        public bool IncludeRevoked { get; init; }

        public GetTeachingAssignmentsByTeacherAndCargoQuery(int teacherId, int cargoId, bool includeRevoked = false)
        {
            TeacherId = teacherId;
            CargoId = cargoId;
            IncludeRevoked = includeRevoked;
        }
    }
}

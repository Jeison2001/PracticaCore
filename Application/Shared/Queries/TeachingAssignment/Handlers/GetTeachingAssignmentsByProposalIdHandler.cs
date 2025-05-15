using Application.Shared.DTOs.TeachingAssignment;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries.TeachingAssignment.Handlers
{
    public class GetTeachingAssignmentsByProposalIdHandler : IRequestHandler<GetTeachingAssignmentsByProposalIdQuery, List<TeachingAssignmentTeacherDto>>
    {
        private readonly ITeachingAssignmentRepository _teachingAssignmentRepository;

        public GetTeachingAssignmentsByProposalIdHandler(
            ITeachingAssignmentRepository teachingAssignmentRepository)
        {
            _teachingAssignmentRepository = teachingAssignmentRepository;
        }        public async Task<List<TeachingAssignmentTeacherDto>> Handle(GetTeachingAssignmentsByProposalIdQuery request, CancellationToken cancellationToken)
        {            
            var assignmentsWithDetails = await _teachingAssignmentRepository.GetTeachingAssignmentsByProposalWithDetailsAsync(
                request.ProposalId,
                request.StatusRegister,
                cancellationToken
            );
            var result = assignmentsWithDetails.Select(detail => new TeachingAssignmentTeacherDto
            {
                Id = detail.TeachingAssignment.Id,
                IdUser = detail.TeachingAssignment.IdTeacher,
                FullName = detail.Teacher != null ? $"{detail.Teacher.FirstName} {detail.Teacher.LastName}" : string.Empty,
                Email = detail.Teacher?.Email ?? string.Empty,
                IdTypeTeachingAssignment = detail.TeachingAssignment.IdTypeTeachingAssignment,
                AssignmentType = detail.TypeTeachingAssignment?.Name ?? string.Empty,
                StatusRegister = detail.TeachingAssignment.StatusRegister
            }).ToList();
            return result;
        }
    }
}
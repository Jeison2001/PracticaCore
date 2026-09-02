using Application.Shared.DTOs.TeachingAssignments;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.TeachingAssignments.Handlers
{
    public class GetTeachingAssignmentsByTeacherQueryHandler : IRequestHandler<GetTeachingAssignmentsByTeacherQuery, List<TeacherCargoGroupDto>>
    {
        private readonly ITeachingAssignmentRepository _teachingAssignmentRepository;

        public GetTeachingAssignmentsByTeacherQueryHandler(ITeachingAssignmentRepository teachingAssignmentRepository)
        {
            _teachingAssignmentRepository = teachingAssignmentRepository;
        }

        public async Task<List<TeacherCargoGroupDto>> Handle(GetTeachingAssignmentsByTeacherQuery request, CancellationToken cancellationToken)
        {
            var assignments = await _teachingAssignmentRepository.GetAssignmentsByTeacherAsync(
                request.TeacherId,
                request.CargoId,
                request.IncludeRevoked,
                cancellationToken
            );

            return assignments
                .GroupBy(ta => ta.IdTypeTeachingAssignment)
                .Select(g =>
                {
                    var first = g.First();
                    var typeTeaching = first.TypeTeachingAssignment;
                    var projects = g.Select(ta => new TeacherAssignedProjectDto
                    {
                        Id = ta.Id,
                        IdInscriptionModality = ta.IdInscriptionModality,
                        IdModality = ta.InscriptionModality.IdModality,
                        ModalityCode = ta.InscriptionModality.Modality?.Code ?? string.Empty,
                        ModalityName = ta.InscriptionModality.Modality?.Name ?? string.Empty,
                        IdStateInscription = ta.InscriptionModality.IdStateInscription,
                        StateInscriptionCode = ta.InscriptionModality.StateInscription?.Code ?? string.Empty,
                        StateInscriptionName = ta.InscriptionModality.StateInscription?.Name ?? string.Empty,
                        IdTypeTeachingAssignment = ta.IdTypeTeachingAssignment,
                        CargoCode = typeTeaching?.Code ?? string.Empty,
                        CargoName = typeTeaching?.Name ?? string.Empty,
                        StatusRegister = ta.StatusRegister,
                        RevocationDate = ta.RevocationDate,
                        AssignedAt = ta.CreatedAt
                    }).ToList();

                    return new TeacherCargoGroupDto
                    {
                        IdTypeTeachingAssignment = g.Key,
                        CargoCode = typeTeaching?.Code ?? string.Empty,
                        CargoName = typeTeaching?.Name ?? string.Empty,
                        MaxAssignments = typeTeaching?.MaxAssignments,
                        ActiveAssignmentsCount = projects.Count(p => p.StatusRegister && p.RevocationDate == null),
                        Assignments = projects
                    };
                })
                .OrderBy(group => group.CargoName)
                .ToList();
        }
    }
}

using Application.Shared.DTOs.TeachingAssignments;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.TeachingAssignments.Handlers
{
    public class GetTeachingAssignmentsByTeacherAndCargoQueryHandler : IRequestHandler<GetTeachingAssignmentsByTeacherAndCargoQuery, List<TeacherAssignedProjectDto>>
    {
        private readonly ITeachingAssignmentRepository _teachingAssignmentRepository;

        public GetTeachingAssignmentsByTeacherAndCargoQueryHandler(ITeachingAssignmentRepository teachingAssignmentRepository)
        {
            _teachingAssignmentRepository = teachingAssignmentRepository;
        }

        public async Task<List<TeacherAssignedProjectDto>> Handle(GetTeachingAssignmentsByTeacherAndCargoQuery request, CancellationToken cancellationToken)
        {
            var assignments = await _teachingAssignmentRepository.GetAssignmentsByTeacherAndCargoAsync(
                request.TeacherId,
                request.CargoId,
                request.IncludeRevoked,
                cancellationToken
            );

            return assignments.Select(ta => new TeacherAssignedProjectDto
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
                CargoCode = ta.TypeTeachingAssignment?.Code ?? string.Empty,
                CargoName = ta.TypeTeachingAssignment?.Name ?? string.Empty,
                StatusRegister = ta.StatusRegister,
                RevocationDate = ta.RevocationDate,
                AssignedAt = ta.CreatedAt
            }).ToList();
        }
    }
}

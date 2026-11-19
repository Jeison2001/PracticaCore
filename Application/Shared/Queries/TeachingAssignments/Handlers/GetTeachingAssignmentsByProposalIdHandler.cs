using Application.Shared.DTOs.TeachingAssignments;
using Domain.Common.Users;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.TeachingAssignments.Handlers
{
    public class GetTeachingAssignmentsByProposalIdHandler : IRequestHandler<GetTeachingAssignmentsByProposalIdQuery, List<TeachingAssignmentTeacherDto>>
    {
        private readonly ITeachingAssignmentRepository _teachingAssignmentRepository;
        private readonly IUnitOfWork _unitOfWork;

        public GetTeachingAssignmentsByProposalIdHandler(
            ITeachingAssignmentRepository teachingAssignmentRepository,
            IUnitOfWork unitOfWork)
        {
            _teachingAssignmentRepository = teachingAssignmentRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<TeachingAssignmentTeacherDto>> Handle(GetTeachingAssignmentsByProposalIdQuery request, CancellationToken cancellationToken)
        {
            await EnsureUserCanReadInscriptionAssignmentsAsync(request.ProposalId, request.CurrentUser, cancellationToken);

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
                StatusRegister = detail.TeachingAssignment.StatusRegister,
                IsConfirmed = detail.TeachingAssignment.IsConfirmed
            }).ToList();
            return result;
        }

        private async Task EnsureUserCanReadInscriptionAssignmentsAsync(
            int inscriptionId,
            CurrentUserInfo? currentUser,
            CancellationToken cancellationToken)
        {
            if (currentUser?.UserId is not int userId || userId <= 0)
                throw new ForbiddenAccessException("Usuario no autenticado. No tienes permiso para consultar las asignaciones docentes.");

            IEnumerable<string> permissions = currentUser.Permissions ?? (IEnumerable<string>)Array.Empty<string>();

            var inscription = await _unitOfWork.GetRepository<InscriptionModality, int>()
                .GetFirstOrDefaultAsync(x => x.Id == inscriptionId && x.StatusRegister, cancellationToken);

            if (inscription == null)
            {
                if (permissions.Any(ModalityPermissionCatalog.AnyModalityTeachingAssignmentRead.Contains))
                    return;

                throw new ForbiddenAccessException("No tienes permiso para consultar las asignaciones docentes de esta inscripción.");
            }

            var isStudentInInscription = await _unitOfWork.GetRepository<UserInscriptionModality, int>().AnyAsync(
                x => x.IdInscriptionModality == inscriptionId &&
                     x.IdUser == userId &&
                     x.StatusRegister &&
                     x.IsConfirmed,
                cancellationToken);

            if (isStudentInInscription)
                return;

            var isConfirmedTeacher = await _unitOfWork.GetRepository<TeachingAssignment, int>().AnyAsync(
                x => x.IdInscriptionModality == inscriptionId &&
                     x.IdTeacher == userId &&
                     x.StatusRegister &&
                     x.RevocationDate == null &&
                     x.IsConfirmed,
                cancellationToken);

            if (isConfirmedTeacher)
                return;

            var modality = await _unitOfWork.GetRepository<Modality, int>()
                .GetFirstOrDefaultAsync(x => x.Id == inscription.IdModality && x.StatusRegister, cancellationToken);

            if (modality != null &&
                !string.IsNullOrWhiteSpace(modality.Code) &&
                ModalityPermissionCatalog.TeachingAssignmentReadByModality.TryGetValue(modality.Code, out var allowedModalityPermissions) &&
                permissions.Any(allowedModalityPermissions.Contains))
            {
                return;
            }

            throw new ForbiddenAccessException("No tienes permiso para consultar las asignaciones docentes de esta inscripción.");
        }
    }
}
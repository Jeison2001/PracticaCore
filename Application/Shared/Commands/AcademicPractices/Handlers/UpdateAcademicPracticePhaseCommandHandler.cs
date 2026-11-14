using Domain.Common.Users;
using Domain.Constants;
using Domain.Exceptions;
using Domain.Interfaces.Services.Jobs;
using MediatR;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Application.Common.Services;
using Application.Common.Services.Jobs;

namespace Application.Shared.Commands.AcademicPractices.Handlers
{
    /// <summary>
    /// Avanza AcademicPractice a una nueva fase/estado. Delega la transición al método
    /// UpdateAcademicPracticeStateAsync del repositorio que aplica reglas de negocio y
    /// establece fechas de fase (AvalApprovalDate, DevelopmentCompletionDate, etc.).
    /// Encola HandleAcademicPracticeChangeAsync cuando el estado cambia para disparar notificaciones.
    /// </summary>
    public class UpdateAcademicPracticePhaseCommandHandler : IRequestHandler<UpdateAcademicPracticePhaseCommand, bool>
    {
        private readonly IAcademicPracticeRepository _academicPracticeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJobEnqueuer _jobEnqueuer;

        public UpdateAcademicPracticePhaseCommandHandler(
            IAcademicPracticeRepository academicPracticeRepository,
            IUnitOfWork unitOfWork,
            IJobEnqueuer jobEnqueuer)
        {
            _academicPracticeRepository = academicPracticeRepository;
            _unitOfWork = unitOfWork;
            _jobEnqueuer = jobEnqueuer;
        }

        public async Task<bool> Handle(UpdateAcademicPracticePhaseCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            var entity = await _academicPracticeRepository.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            // Crear copia del estado original para comparación
            var original = new AcademicPractice { Id = entity.Id, IdStateStage = entity.IdStateStage };
            var originalObservations = entity.Observations;
            var originalEvaluatorObservations = entity.EvaluatorObservations;

            await EnsureUserAuthorizedForPhaseUpdateAsync(entity, dto.NewStateStageId, request.CurrentUser, cancellationToken);

            await ModalityStateHelper.ValidateStateTransitionAndCheckRejectionAsync(
                _unitOfWork,
                entity.Id,
                entity.IdStateStage,
                dto.NewStateStageId,
                cancellationToken);

            var result = await _academicPracticeRepository.UpdateAcademicPracticeStateAsync(
                dto.Id,
                dto.NewStateStageId,
                dto.Observations,
                dto.EvaluatorObservations,
                cancellationToken);

            if (!result) return false;

            // Registrar historial de observaciones (general y del evaluador) solo si hay cambio real
            var evaluatorId = request.CurrentUser.UserId 
                ?? entity.IdUserUpdatedAt 
                ?? entity.IdUserCreatedAt 
                ?? 1;

            await ObservationHistoryHelper.CreateIfChangedAsync(
                _unitOfWork,
                entityType: nameof(AcademicPractice),
                entityId: entity.Id,
                evaluatorId: evaluatorId,
                evaluationTypeCode: EvaluationTypeCodes.AcademicPractice,
                newObservations: dto.EvaluatorObservations,
                previousObservations: originalEvaluatorObservations,
                cancellationToken: cancellationToken);

            if (!string.Equals(dto.Observations?.Trim(), dto.EvaluatorObservations?.Trim(), StringComparison.Ordinal))
            {
                await ObservationHistoryHelper.CreateIfChangedAsync(
                    _unitOfWork,
                    entityType: nameof(AcademicPractice),
                    entityId: entity.Id,
                    evaluatorId: evaluatorId,
                    evaluationTypeCode: EvaluationTypeCodes.AcademicPractice,
                    newObservations: dto.Observations,
                    previousObservations: originalObservations,
                    cancellationToken: cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            // Verificar si realmente hubo cambio de estado y procesar notificación en background
            if (original.IdStateStage != dto.NewStateStageId)
            {
                ProcessNotificationsAsync(dto.Id, original.IdStateStage);
            }

            return true;
        }

        private async Task EnsureUserAuthorizedForPhaseUpdateAsync(
            AcademicPractice entity,
            int newStateStageId,
            CurrentUserInfo currentUser,
            CancellationToken cancellationToken)
        {
            if (currentUser.UserId is not int userId || userId <= 0 || currentUser.Permissions is not { Count: > 0 } perms)
                return;

            var isManagementOrAdmin =
                perms.Contains(PermissionCodes.PracticaAcademica.N4PAF0PG) ||
                perms.Contains(PermissionCodes.PracticaAcademica.N4PAF0PA) ||
                perms.Contains(PermissionCodes.PracticaAcademica.N4PAF1PG) ||
                perms.Contains(PermissionCodes.PracticaAcademica.N4PAF2PG) ||
                perms.Contains(PermissionCodes.Configuraciones.N3CFSU) ||
                perms.Contains(PermissionCodes.ModalidadesGrado.N3MGPR);

            if (isManagementOrAdmin)
                return;

            var isEvaluator =
                perms.Contains(PermissionCodes.PracticaAcademica.N4PAF0PR) ||
                perms.Contains(PermissionCodes.PracticaAcademica.N4PAF1PR) ||
                perms.Contains(PermissionCodes.PracticaAcademica.N4PAF2PR);

            if (isEvaluator)
            {
                var taRepo = _unitOfWork.GetRepository<TeachingAssignment, int>();
                var isConfirmedAssigned = await taRepo.AnyAsync(
                    x => x.IdInscriptionModality == entity.Id &&
                         x.IdTeacher == userId &&
                         x.StatusRegister &&
                         x.RevocationDate == null &&
                         x.IsConfirmed,
                    cancellationToken);

                if (isConfirmedAssigned)
                    return;

                var hasAnyAssignment = await taRepo.AnyAsync(
                    x => x.IdInscriptionModality == entity.Id && x.IdTeacher == userId,
                    cancellationToken);

                if (hasAnyAssignment)
                    throw new ForbiddenAccessException("No tienes una asignación docente activa y confirmada para evaluar esta práctica académica.");

                if (!perms.Contains(PermissionCodes.PracticaAcademica.N4PAF0PR))
                    throw new ForbiddenAccessException("No estás asignado como docente evaluador confirmado de esta práctica académica.");

                var currentState = await _unitOfWork.GetRepository<StateStage, int>().GetByIdAsync(entity.IdStateStage);
                if (currentState != null && currentState.Code == StateStageCodes.PaInscripcionPendDoc)
                    return;

                if (await IsCommitteeDirectorOrSecretaryAsync(userId, cancellationToken))
                    return;

                throw new ForbiddenAccessException("No estás autorizado para evaluar esta práctica académica fuera de la revisión inicial de inscripción.");
            }

            // Estudiante (N3PAF1R / N3PAF2R): validar membresía activa y estado destino no final/rechazo
            var uimRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
            var isMember = await uimRepo.AnyAsync(
                x => x.IdInscriptionModality == entity.Id &&
                     x.IdUser == userId &&
                     x.StatusRegister,
                cancellationToken);

            if (!isMember)
                throw new ForbiddenAccessException("No estás vinculado como estudiante a esta práctica académica.");

            var newState = await _unitOfWork.GetRepository<StateStage, int>().GetByIdAsync(newStateStageId);
            if (newState == null ||
                newState.IsFinalStateForStage ||
                newState.IsFinalStateForModalityOverall ||
                newState.IsRejectionState)
            {
                throw new ForbiddenAccessException("Los estudiantes no pueden aprobar, finalizar ni rechazar una fase de práctica académica.");
            }
        }

        private async Task<bool> IsCommitteeDirectorOrSecretaryAsync(int userId, CancellationToken cancellationToken)
        {
            var userRoleRepo = _unitOfWork.GetRepository<UserRole, int>();
            var activeUserRoles = await userRoleRepo.GetAllAsync(ur => ur.IdUser == userId && ur.StatusRegister);
            var roleIds = activeUserRoles.Select(ur => ur.IdRole).Distinct().ToList();
            if (roleIds.Count == 0)
                return false;

            var roleRepo = _unitOfWork.GetRepository<Role, int>();
            return await roleRepo.AnyAsync(
                r => roleIds.Contains(r.Id) &&
                     r.StatusRegister &&
                     (r.Code == "COMMITTEE_DIRECTOR" || r.Code == "SECRETARY"),
                cancellationToken);
        }

        private void ProcessNotificationsAsync(int practiceId, int oldStateId)
        {
            // Fire-and-forget seguro usando Hangfire
            _jobEnqueuer.Enqueue<INotificationBackgroundJob>(
                x => x.HandleAcademicPracticeChangeAsync(practiceId, oldStateId));
        }
    }
}

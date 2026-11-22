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

            var isAdvancingFromApprovedPreviousStage =
                await EnsureUserAuthorizedForPhaseUpdateAsync(entity, dto.NewStateStageId, request.CurrentUser, cancellationToken);

            await ModalityStateHelper.ValidateStateTransitionAndCheckRejectionAsync(
                _unitOfWork,
                entity.Id,
                entity.IdStateStage,
                dto.NewStateStageId,
                allowCrossStageWithinSameModality: isAdvancingFromApprovedPreviousStage,
                cancellationToken);

            var result = await _academicPracticeRepository.UpdateAcademicPracticeStateAsync(
                dto.Id,
                dto.NewStateStageId,
                dto.Observations,
                dto.EvaluatorObservations,
                cancellationToken);

            if (!result) return false;

            // Registrar historial de observaciones (general y del evaluador) solo si hay cambio real
            var evaluatorId = request.CurrentUser?.UserId ?? 0;

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

        private async Task<bool> EnsureUserAuthorizedForPhaseUpdateAsync(
            AcademicPractice entity,
            int newStateStageId,
            CurrentUserInfo? currentUser,
            CancellationToken cancellationToken)
        {
            if (currentUser?.UserId is not int userId ||
                userId <= 0 ||
                currentUser.Permissions is not { Count: > 0 } perms)
            {
                throw new ForbiddenAccessException("No tiene permisos vigentes para actualizar la fase de la práctica académica.");
            }

            var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();
            var stageModalityRepo = _unitOfWork.GetRepository<StageModality, int>();

            var currentState = await stateStageRepo.GetByIdAsync(entity.IdStateStage);
            if (currentState == null || currentState.IdStageModality <= 0)
            {
                throw new ForbiddenAccessException("La práctica académica no tiene una etapa válida configurada para autorizar el cambio de fase.");
            }

            var currentStage = await stageModalityRepo.GetByIdAsync(currentState.IdStageModality);
            if (currentStage == null ||
                string.IsNullOrWhiteSpace(currentStage.Code) ||
                !ModalityPermissionCatalog.PracticaAcademicaPhaseManagementByStage.ContainsKey(currentStage.Code))
            {
                throw new ForbiddenAccessException("La etapa actual de la práctica académica no es válida para autorización por fase.");
            }

            var newState = await stateStageRepo.GetByIdAsync(newStateStageId);
            StageModality? targetStage = null;
            if (newState != null)
            {
                if (newState.IdStageModality <= 0)
                {
                    throw new ForbiddenAccessException("El estado destino no tiene una etapa válida configurada.");
                }

                targetStage = newState.IdStageModality == currentState.IdStageModality
                    ? currentStage
                    : await stageModalityRepo.GetByIdAsync(newState.IdStageModality);

                if (targetStage == null ||
                    string.IsNullOrWhiteSpace(targetStage.Code) ||
                    !ModalityPermissionCatalog.PracticaAcademicaPhaseManagementByStage.ContainsKey(targetStage.Code))
                {
                    throw new ForbiddenAccessException("La etapa destino no pertenece a una fase válida de Práctica Académica.");
                }
            }

            bool isAdvancingFromApprovedPreviousStage =
                currentState.IsFinalStateForStage &&
                !currentState.IsFinalStateForModalityOverall &&
                !currentState.IsRejectionState &&
                targetStage != null &&
                !string.Equals(targetStage.Code, currentStage.Code, StringComparison.OrdinalIgnoreCase) &&
                targetStage.IdModality == currentStage.IdModality &&
                targetStage.StageOrder == currentStage.StageOrder + 1;

            if (isAdvancingFromApprovedPreviousStage)
            {
                await EnsureAuthorizedForStageAsync(
                    entity.Id,
                    userId,
                    perms,
                    targetStage!.Code,
                    newState,
                    cancellationToken);

                return true;
            }

            await EnsureAuthorizedForStageAsync(
                entity.Id,
                userId,
                perms,
                currentStage.Code,
                newState,
                cancellationToken);

            if (targetStage != null &&
                !string.Equals(targetStage.Code, currentStage.Code, StringComparison.OrdinalIgnoreCase))
            {
                await EnsureAuthorizedForStageAsync(
                    entity.Id,
                    userId,
                    perms,
                    targetStage.Code,
                    newState,
                    cancellationToken);
            }

            return false;
        }

        private async Task EnsureAuthorizedForStageAsync(
            int practiceId,
            int userId,
            IReadOnlyCollection<string> perms,
            string stageCode,
            StateStage? newState,
            CancellationToken cancellationToken)
        {
            if (ModalityPermissionCatalog.PracticaAcademicaPhaseManagementByStage.TryGetValue(stageCode, out var managementPerms) &&
                perms.Any(managementPerms.Contains))
            {
                return;
            }

            if (ModalityPermissionCatalog.PracticaAcademicaPhaseEvaluatorByStage.TryGetValue(stageCode, out var evaluatorPerms) &&
                perms.Any(evaluatorPerms.Contains))
            {
                var taRepo = _unitOfWork.GetRepository<TeachingAssignment, int>();
                var isConfirmedAssigned = await taRepo.AnyAsync(
                    x => x.IdInscriptionModality == practiceId &&
                         x.IdTeacher == userId &&
                         x.StatusRegister &&
                         x.RevocationDate == null &&
                         x.IsConfirmed,
                    cancellationToken);

                if (isConfirmedAssigned)
                    return;

                var hasAnyAssignment = await taRepo.AnyAsync(
                    x => x.IdInscriptionModality == practiceId && x.IdTeacher == userId,
                    cancellationToken);

                if (hasAnyAssignment)
                    throw new ForbiddenAccessException("No tienes una asignación docente activa y confirmada para evaluar esta práctica académica.");

                throw new ForbiddenAccessException("No estás asignado como docente evaluador confirmado de esta práctica académica.");
            }

            if (ModalityPermissionCatalog.PracticaAcademicaPhaseStudentByStage.TryGetValue(stageCode, out var studentPerms) &&
                studentPerms.Count > 0 &&
                perms.Any(studentPerms.Contains))
            {
                var uimRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
                var isMember = await uimRepo.AnyAsync(
                    x => x.IdInscriptionModality == practiceId &&
                         x.IdUser == userId &&
                         x.StatusRegister &&
                         x.IsConfirmed,
                    cancellationToken);

                if (!isMember)
                    throw new ForbiddenAccessException("No estás vinculado como estudiante a esta práctica académica.");

                if (newState != null &&
                    (newState.IsFinalStateForStage ||
                     newState.IsFinalStateForModalityOverall ||
                     newState.IsRejectionState))
                {
                    throw new ForbiddenAccessException("Los estudiantes no pueden aprobar, finalizar ni rechazar una fase de práctica académica.");
                }

                return;
            }

            throw new ForbiddenAccessException($"No tienes permisos vigentes para operar sobre la etapa '{stageCode}' de la práctica académica.");
        }

        private void ProcessNotificationsAsync(int practiceId, int oldStateId)
        {
            // Fire-and-forget seguro usando Hangfire
            _jobEnqueuer.Enqueue<INotificationBackgroundJob>(
                x => x.HandleAcademicPracticeChangeAsync(practiceId, oldStateId));
        }
    }
}

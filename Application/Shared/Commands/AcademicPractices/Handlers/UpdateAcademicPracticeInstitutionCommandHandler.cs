using Application.Shared.DTOs.AcademicPractices;
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
    /// Actualiza campos de institución (título, nombre, contacto, fechas, horas, observaciones)
    /// de una AcademicPractice. Opcionalmente transiciona a un nuevo estado si NewStateStageId
    /// está presente (ej: después de radicar documentos). Encola HandleAcademicPracticeChangeAsync
    /// si el estado cambia.
    /// </summary>
    public class UpdateAcademicPracticeInstitutionCommandHandler : IRequestHandler<UpdateAcademicPracticeInstitutionCommand, bool>
    {
        private readonly IAcademicPracticeRepository _academicPracticeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJobEnqueuer _jobEnqueuer;

        public UpdateAcademicPracticeInstitutionCommandHandler(
            IAcademicPracticeRepository academicPracticeRepository,
            IUnitOfWork unitOfWork,
            IJobEnqueuer jobEnqueuer)
        {
            _academicPracticeRepository = academicPracticeRepository;
            _unitOfWork = unitOfWork;
            _jobEnqueuer = jobEnqueuer;
        }

        public async Task<bool> Handle(UpdateAcademicPracticeInstitutionCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            var academicPractice = await _academicPracticeRepository.GetByIdAsync(dto.Id);
            if (academicPractice == null)
                return false;

            // Snapshot para notificaciones y deduplicación de observaciones
            var originalStateId = academicPractice.IdStateStage;
            var originalObservations = academicPractice.Observations;
            var hasStateChange = dto.NewStateStageId.HasValue
                && dto.NewStateStageId.Value > 0
                && dto.NewStateStageId.Value != originalStateId;

            if (request.CurrentUser?.UserId is not > 0 ||
                request.CurrentUser.Permissions is not { Count: > 0 } perms)
            {
                throw new ForbiddenAccessException("No tiene permisos vigentes para actualizar la información de la institución.");
            }

            var isInstitutionManager = perms.Any(ModalityPermissionCatalog.PracticaAcademicaInstitutionManagers.Contains);
            var isStudentActor = !isInstitutionManager;

            if (isStudentActor)
            {
                await ModalityAccessAuthorizationHelper.EnsureStudentAuthorizedForInscriptionAsync(
                    _unitOfWork,
                    academicPractice.Id,
                    hasStateChange ? dto.NewStateStageId : null,
                    request.CurrentUser,
                    "No está autorizado para actualizar la institución de otra práctica académica.",
                    "El estudiante no tiene permisos para aprobar o rechazar su propia práctica académica.",
                    cancellationToken);
            }

            await ValidatePracticeOpenAndEditableAsync(
                academicPractice.Id,
                originalStateId,
                hasStateChange,
                isStudentActor,
                cancellationToken);

            if (hasStateChange)
            {
                await ModalityStateHelper.ValidateStateTransitionAndCheckRejectionAsync(
                    _unitOfWork,
                    academicPractice.Id,
                    originalStateId,
                    dto.NewStateStageId!.Value,
                    allowCrossStageWithinSameModality: !isStudentActor,
                    cancellationToken);
            }

            // Actualizar campos de institución
            UpdateInstitutionFields(academicPractice, dto);

            // Registrar historial de observaciones solo si hay texto nuevo/distinto
            if (!string.IsNullOrWhiteSpace(dto.Observations))
            {
                var evaluatorId = request.CurrentUser?.UserId ?? 0;

                await ObservationHistoryHelper.CreateIfChangedAsync(
                    _unitOfWork,
                    entityType: nameof(AcademicPractice),
                    entityId: academicPractice.Id,
                    evaluatorId: evaluatorId,
                    evaluationTypeCode: EvaluationTypeCodes.AcademicPractice,
                    newObservations: dto.Observations,
                    previousObservations: originalObservations,
                    cancellationToken: cancellationToken);
            }

            // Cambiar de estado si se especifica (ej: pasar a PA_PEND_APROBACION_DOCUMENTOS después de radicar docs)
            if (hasStateChange)
            {
                academicPractice.IdStateStage = dto.NewStateStageId!.Value;
            }

            // Tracking
            UpdateTrackingFields(academicPractice, dto, request.CurrentUser?.UserId);

            await _academicPracticeRepository.UpdateAsync(academicPractice);
            await _unitOfWork.CommitAsync(cancellationToken);

            // Encolar job de notificación si hubo cambio de estado (patrón consistente)
            if (hasStateChange)
            {
                _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x =>
                    x.HandleAcademicPracticeChangeAsync(academicPractice.Id, originalStateId));
            }

            return true;
        }

        private static void UpdateInstitutionFields(AcademicPractice academicPractice, UpdateInstitutionInfoDto dto)
        {
            if (!string.IsNullOrEmpty(dto.Title))
                academicPractice.Title = dto.Title;
                
            if (!string.IsNullOrEmpty(dto.InstitutionName))
                academicPractice.InstitutionName = dto.InstitutionName;
                
            if (!string.IsNullOrEmpty(dto.InstitutionContact))
                academicPractice.InstitutionContact = dto.InstitutionContact;
                
            if (dto.PracticeStartDate.HasValue)
                academicPractice.PracticeStartDate = dto.PracticeStartDate.Value;

            if (dto.PracticeEndDate.HasValue)
                academicPractice.PracticeEndDate = dto.PracticeEndDate.Value;

            if (dto.PracticeHours.HasValue)
                academicPractice.PracticeHours = dto.PracticeHours;
                
            academicPractice.IsEmprendimiento = dto.IsEmprendimiento;
                
            if (!string.IsNullOrEmpty(dto.Observations))
                academicPractice.Observations = dto.Observations;
        }

        private async Task ValidatePracticeOpenAndEditableAsync(
            int inscriptionId,
            int originalStateId,
            bool hasStateChange,
            bool isStudentActor,
            CancellationToken cancellationToken)
        {
            var inscription = await _unitOfWork.GetRepository<InscriptionModality, int>().GetByIdAsync(inscriptionId);
            if (inscription != null)
            {
                if (inscription.IsFinalRejection)
                {
                    throw new InvalidOperationException(
                        "No se puede actualizar la información de institución porque la modalidad tiene un rechazo definitivo.");
                }

                if (inscription.IdStateInscription > 0)
                {
                    var stateInscription = await _unitOfWork.GetRepository<StateInscription, int>()
                        .GetByIdAsync(inscription.IdStateInscription);
                    if (stateInscription != null &&
                        (stateInscription.IsRejectionState ||
                         string.Equals(stateInscription.Code, StateInscriptionCodes.Rechazado, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(stateInscription.Code, StateInscriptionCodes.NoAplica, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException(
                            $"No se puede actualizar la información de institución porque la inscripción se encuentra en estado '{stateInscription.Name}'.");
                    }
                }
            }

            var currentState = await _unitOfWork.GetRepository<StateStage, int>().GetByIdAsync(originalStateId);

            if (!hasStateChange && currentState != null &&
                (currentState.IsFinalStateForStage ||
                 currentState.IsFinalStateForModalityOverall ||
                 currentState.IsRejectionState))
            {
                throw new InvalidOperationException(
                    $"No se puede actualizar la información de institución porque la práctica ya se encuentra en estado '{currentState.Name}'.");
            }

            if (!isStudentActor)
                return;

            if (currentState == null)
            {
                throw new InvalidOperationException(
                    "No se puede actualizar la información de institución porque el estado actual de la práctica no es válido.");
            }

            if (currentState.IsFinalStateForStage ||
                currentState.IsFinalStateForModalityOverall ||
                currentState.IsRejectionState)
            {
                throw new InvalidOperationException(
                    $"No se puede actualizar la información de institución porque la práctica ya se encuentra en estado '{currentState.Name}'.");
            }

            if (currentState.IdStageModality > 0)
            {
                var currentStage = await _unitOfWork.GetRepository<StageModality, int>()
                    .GetByIdAsync(currentState.IdStageModality);
                if (currentStage != null &&
                    !string.Equals(currentStage.Code, StageModalityCodes.PaFaseInscripcion, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "El estudiante solo puede actualizar la información de institución durante la Fase 0 (Inscripción).");
                }
            }

            if (!IsStudentEditablePhase0State(currentState.Code))
            {
                throw new InvalidOperationException(
                    $"El estudiante no puede actualizar la información de institución mientras la práctica está en estado '{currentState.Name}'.");
            }
        }

        private static bool IsStudentEditablePhase0State(string? stateCode) =>
            string.Equals(stateCode, StateStageCodes.PaInscripcionPendDoc, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(stateCode, StateStageCodes.PaInscripcionObservaciones, StringComparison.OrdinalIgnoreCase);

        private static void UpdateTrackingFields(AcademicPractice academicPractice, UpdateInstitutionInfoDto dto, int? userId)
        {
            academicPractice.UpdatedAt = DateTimeOffset.UtcNow;
            academicPractice.IdUserUpdatedAt = userId;
        }

    }
}


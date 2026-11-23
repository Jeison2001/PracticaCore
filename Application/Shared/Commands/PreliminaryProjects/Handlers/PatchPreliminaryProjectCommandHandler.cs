using Application.Common.Services;
using Application.Common.Services.Jobs;
using Application.Shared.DTOs.PreliminaryProjects;
using AutoMapper;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Shared.Commands.PreliminaryProjects.Handlers
{
    /// <summary>
    /// Actualización parcial de PreliminaryProject: solo IdStateStage, ApprovalDate y Observations
    /// pueden ser modificados. Encola HandlePreliminaryProjectChangeAsync cuando el estado cambia,
    /// que dispara notificaciones PRELIMINARY_APPROVED o PRELIMINARY_REJECTED según el nuevo estado.
    /// </summary>
    public class PatchPreliminaryProjectCommandHandler : IRequestHandler<PatchPreliminaryProjectCommand, PreliminaryProjectDto>
    {
        private readonly IRepository<PreliminaryProject, int> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IJobEnqueuer _jobEnqueuer;
        private readonly ILogger<PatchPreliminaryProjectCommandHandler> _logger;

        public PatchPreliminaryProjectCommandHandler(
            IRepository<PreliminaryProject, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IJobEnqueuer jobEnqueuer,
            ILogger<PatchPreliminaryProjectCommandHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _jobEnqueuer = jobEnqueuer;
            _logger = logger;
        }

        public async Task<PreliminaryProjectDto> Handle(PatchPreliminaryProjectCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null)
                throw new KeyNotFoundException($"PreliminaryProject with ID {request.Id} not found.");

            var originalStateId = entity.IdStateStage;
            var originalObservations = entity.Observations;

            await ModalityAccessAuthorizationHelper.EnsureEvaluatorOrStudentAuthorizedForPatchAsync(
                _unitOfWork,
                entity.Id,
                request.Dto.IdStateStage,
                request.CurrentUser,
                [],
                PermissionCodes.ProyectoGrado.N4PGPAR,
                "El docente no tiene una asignación confirmada activa en este anteproyecto.",
                "No está autorizado para modificar un anteproyecto al cual no pertenece.",
                "El estudiante no tiene permisos para aprobar o rechazar su propio anteproyecto.",
                cancellationToken);

            _logger.LogInformation(
                "Patching PreliminaryProject Id={Id}. Changes: StateStage={State}, ApprovalDate={Date}, Observations={Obs}",
                request.Id,
                request.Dto.IdStateStage,
                request.Dto.ApprovalDate,
                request.Dto.Observations);

            var updatedProperties = new List<Expression<Func<PreliminaryProject, object?>>>();

            // Siempre actualizar campos de tracking primero
            entity.IdUserUpdatedAt = request.CurrentUser.UserId;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            updatedProperties.Add(x => x.IdUserUpdatedAt);
            updatedProperties.Add(x => x.UpdatedAt);

            // Actualizar estado si se provee
            if (request.Dto.IdStateStage.HasValue)
            {
                await ModalityStateHelper.ValidateStateTransitionAndCheckRejectionAsync(
                    _unitOfWork,
                    entity.Id,
                    originalStateId,
                    request.Dto.IdStateStage.Value,
                    cancellationToken);

                var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();
                var currentState = await stateStageRepo.GetByIdAsync(originalStateId);
                var newState = await stateStageRepo.GetByIdAsync(request.Dto.IdStateStage.Value);

                if (request.Dto.IdStateStage.Value != originalStateId &&
                    newState != null &&
                    (newState.Code == StateStageCodes.ApAprobado ||
                     newState.Code == StateStageCodes.ApConObservaciones ||
                     newState.IsRejectionState))
                {
                    if (currentState != null &&
                        (currentState.Code == StateStageCodes.ApPendienteDocumento ||
                         currentState.Code == StateStageCodes.ApRadicadoPendAsigEval))
                    {
                        throw new InvalidOperationException(
                            "El anteproyecto debe estar en estado En Evaluación para registrar un dictamen.");
                    }

                    var taRepo = _unitOfWork.GetRepository<TeachingAssignment, int>();
                    var confirmedJuradosCount = await TeachingAssignmentEvaluationHelper.CountConfirmedJuradosAsync(
                        _unitOfWork,
                        taRepo,
                        entity.Id,
                        knownJuradoTypeId: null,
                        cancellationToken);

                    if (confirmedJuradosCount < TeachingAssignmentEvaluationHelper.MinimumConfirmedJurados)
                    {
                        throw new InvalidOperationException(
                            $"El anteproyecto requiere al menos {TeachingAssignmentEvaluationHelper.MinimumConfirmedJurados} jurados evaluadores confirmados para registrar un dictamen.");
                    }
                }

                entity.IdStateStage = request.Dto.IdStateStage.Value;
                updatedProperties.Add(x => x.IdStateStage);
            }

            // Actualizar fecha de aprobación si se provee
            if (request.Dto.ApprovalDate.HasValue)
            {
                entity.ApprovalDate = request.Dto.ApprovalDate.Value;
                updatedProperties.Add(x => x.ApprovalDate);
            }

            // Actualizar observaciones si se proveen
            if (request.Dto.Observations != null)
            {
                entity.Observations = request.Dto.Observations;
                updatedProperties.Add(x => x.Observations);
            }

            // Registrar historial de observaciones solo si hay texto nuevo/distinto
            await ObservationHistoryHelper.CreateIfChangedAsync(_unitOfWork,
                entityType: nameof(PreliminaryProject),
                entityId: entity.Id,
                evaluatorId: request.CurrentUser.UserId ?? 0,
                evaluationTypeCode: EvaluationTypeCodes.PreliminaryProject,
                newObservations: request.Dto.Observations,
                previousObservations: originalObservations,
                cancellationToken: cancellationToken,
                effectiveStateStageId: entity.IdStateStage);

            await _repository.UpdatePartialAsync(entity, updatedProperties.ToArray());
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "PreliminaryProject Id={Id} patched successfully. State={State}",
                request.Id, entity.IdStateStage);

            // Disparar notificación si el estado cambió
            if (request.Dto.IdStateStage.HasValue && request.Dto.IdStateStage.Value != originalStateId)
            {
                _logger.LogInformation(
                    "PreliminaryProject state changed from {OldState} to {NewState}. Enqueuing notification job.",
                    originalStateId, request.Dto.IdStateStage.Value);

                _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x =>
                    x.HandlePreliminaryProjectChangeAsync(request.Id, originalStateId));
            }

            return _mapper.Map<PreliminaryProjectDto>(entity);
        }
    }
}

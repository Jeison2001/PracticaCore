using System.Linq.Expressions;
using Application.Shared.DTOs.Evaluations;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Features.Evaluations.Commands.Handlers
{
    /// <summary>
    /// Edita de forma controlada el texto de una observación en <see cref="Evaluation"/>:
    /// solo el evaluador autor puede modificarla, únicamente cuando la entidad asociada
    /// no se encuentra en estado final ni de rechazo definitivo, y sincroniza el campo de
    /// observaciones vigente de la entidad si corresponde al registro más reciente.
    /// </summary>
    public class UpdateEvaluationObservationCommandHandler
        : IRequestHandler<UpdateEvaluationObservationCommand, ObservationHistoryItemDto>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateEvaluationObservationCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ObservationHistoryItemDto> Handle(
            UpdateEvaluationObservationCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = request.CurrentUser?.UserId;
            if (!currentUserId.HasValue || currentUserId.Value <= 0)
                throw new UnauthorizedAccessException("Usuario no autenticado.");

            var evalRepo = _unitOfWork.GetRepository<Evaluation, int>();
            var evaluation = await evalRepo.GetByIdAsync(request.Id);
            if (evaluation == null || !evaluation.StatusRegister)
                throw new KeyNotFoundException($"Evaluation with ID {request.Id} not found.");

            if (evaluation.IdEvaluator != currentUserId.Value)
                throw new ForbiddenAccessException("Solo el autor de la observación puede editarla.");

            var trimmedNew = request.Dto.Observations?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedNew))
                throw new InvalidOperationException("La observación no puede estar vacía.");

            var previousObs = evaluation.Observations?.Trim();
            var syncEntityAction = await EnsureEntityOpenAndPrepareSyncAsync(
                evaluation.EntityType,
                evaluation.EntityId,
                previousObs,
                cancellationToken);

            var userRepo = _unitOfWork.GetRepository<User, int>();
            var evaluator = await userRepo.GetByIdAsync(evaluation.IdEvaluator);
            var typeRepo = _unitOfWork.GetRepository<EvaluationType, int>();
            var evalType = await typeRepo.GetByIdAsync(evaluation.IdEvaluationType);

            if (!string.Equals(previousObs, trimmedNew, StringComparison.Ordinal))
            {
                evaluation.Observations = trimmedNew;
                evaluation.UpdatedAt = DateTimeOffset.UtcNow;
                evaluation.IdUserUpdatedAt = currentUserId.Value;
                evaluation.OperationRegister = "UPDATE_OBSERVATION";

                await evalRepo.UpdatePartialAsync(evaluation, [
                    x => x.Observations,
                    x => x.UpdatedAt,
                    x => x.IdUserUpdatedAt,
                    x => x.OperationRegister
                ]);

                // Sincronizar con la entidad solo si esta observación es la más reciente del historial
                var latestEval = (await evalRepo.GetAllAsync(
                        e => e.StatusRegister
                             && e.EntityType == evaluation.EntityType
                             && e.EntityId == evaluation.EntityId,
                        q => q.OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id)))
                    .FirstOrDefault();

                if (latestEval == null || latestEval.Id == evaluation.Id)
                {
                    await syncEntityAction(trimmedNew, currentUserId.Value);
                }

                await _unitOfWork.CommitAsync(cancellationToken);
            }

            return new ObservationHistoryItemDto
            {
                Id = evaluation.Id,
                EntityId = evaluation.EntityId,
                IdEvaluator = evaluation.IdEvaluator,
                EvaluatorName = evaluator != null ? $"{evaluator.FirstName} {evaluator.LastName}".Trim() : null,
                IdEvaluationType = evaluation.IdEvaluationType,
                EvaluationTypeCode = evalType?.Code,
                EvaluationTypeName = evalType?.Name,
                Result = evaluation.Result,
                Observations = evaluation.Observations,
                CreatedAt = evaluation.CreatedAt,
                UpdatedAt = evaluation.UpdatedAt
            };
        }

        private async Task<Func<string, int, Task>> EnsureEntityOpenAndPrepareSyncAsync(
            string entityType,
            int entityId,
            string? previousObs,
            CancellationToken cancellationToken)
        {
            switch (entityType)
            {
                case nameof(InscriptionModality):
                {
                    var repo = _unitOfWork.GetRepository<InscriptionModality, int>();
                    var entity = await repo.GetByIdAsync(entityId)
                        ?? throw new KeyNotFoundException($"InscriptionModality with ID {entityId} not found.");

                    var state = await _unitOfWork.GetRepository<StateInscription, int>().GetByIdAsync(entity.IdStateInscription);
                    bool isClosed = entity.IsFinalRejection
                        || (state != null && (state.IsFinalStateForStage
                            || state.IsRejectionState
                            || string.Equals(state.Code, StateInscriptionCodes.Aprobado, StringComparison.OrdinalIgnoreCase)));

                    if (isClosed)
                        throw new InvalidOperationException($"No se puede editar la observación porque la inscripción ya se encuentra en estado '{state?.Name ?? "cerrado"}'.");

                    return async (obs, userId) =>
                    {
                        entity.Observations = obs;
                        entity.UpdatedAt = DateTimeOffset.UtcNow;
                        entity.IdUserUpdatedAt = userId;
                        await repo.UpdatePartialAsync(entity, [x => x.Observations, x => x.UpdatedAt, x => x.IdUserUpdatedAt]);
                    };
                }

                case nameof(Proposal):
                {
                    var repo = _unitOfWork.GetRepository<Proposal, int>();
                    var entity = await ValidateStageEntityOpenAsync(repo, entityId, e => e.IdStateStage, e => e.Id);
                    return async (obs, userId) =>
                    {
                        entity.Observation = obs;
                        entity.UpdatedAt = DateTimeOffset.UtcNow;
                        entity.IdUserUpdatedAt = userId;
                        await repo.UpdatePartialAsync(entity, [x => x.Observation, x => x.UpdatedAt, x => x.IdUserUpdatedAt]);
                    };
                }

                case nameof(AcademicPractice):
                {
                    var repo = _unitOfWork.GetRepository<AcademicPractice, int>();
                    var entity = await ValidateStageEntityOpenAsync(repo, entityId, e => e.IdStateStage, e => e.Id);
                    return async (obs, userId) =>
                    {
                        var props = new List<Expression<Func<AcademicPractice, object?>>>
                        {
                            x => x.UpdatedAt,
                            x => x.IdUserUpdatedAt
                        };

                        if (string.Equals(entity.EvaluatorObservations?.Trim(), previousObs, StringComparison.Ordinal))
                        {
                            entity.EvaluatorObservations = obs;
                            props.Add(x => x.EvaluatorObservations);
                        }
                        else if (string.Equals(entity.Observations?.Trim(), previousObs, StringComparison.Ordinal))
                        {
                            entity.Observations = obs;
                            props.Add(x => x.Observations);
                        }
                        else if (!string.IsNullOrWhiteSpace(entity.EvaluatorObservations))
                        {
                            entity.EvaluatorObservations = obs;
                            props.Add(x => x.EvaluatorObservations);
                        }
                        else if (!string.IsNullOrWhiteSpace(entity.Observations))
                        {
                            entity.Observations = obs;
                            props.Add(x => x.Observations);
                        }

                        entity.UpdatedAt = DateTimeOffset.UtcNow;
                        entity.IdUserUpdatedAt = userId;
                        await repo.UpdatePartialAsync(entity, props.ToArray());
                    };
                }

                case nameof(PreliminaryProject):
                    return await PrepareStandardStageEntityAsync<PreliminaryProject>(
                        entityId, e => e.IdStateStage, e => e.Id,
                        (e, obs) => e.Observations = obs, e => e.Observations);

                case nameof(ProjectFinal):
                    return await PrepareStandardStageEntityAsync<ProjectFinal>(
                        entityId, e => e.IdStateStage, e => e.Id,
                        (e, obs) => e.Observations = obs, e => e.Observations);

                case nameof(ProjectPresentation):
                    return await PrepareStandardStageEntityAsync<ProjectPresentation>(
                        entityId, e => e.IdStateStage, e => e.IdInscriptionModality,
                        (e, obs) => e.Observations = obs, e => e.Observations);

                case nameof(AcademicAverage):
                    return await PrepareStandardStageEntityAsync<AcademicAverage>(
                        entityId, e => e.IdStateStage, e => e.Id,
                        (e, obs) => e.Observations = obs, e => e.Observations);

                case nameof(CoTerminal):
                    return await PrepareStandardStageEntityAsync<CoTerminal>(
                        entityId, e => e.IdStateStage, e => e.Id,
                        (e, obs) => e.Observations = obs, e => e.Observations);

                case nameof(Seminar):
                    return await PrepareStandardStageEntityAsync<Seminar>(
                        entityId, e => e.IdStateStage, e => e.Id,
                        (e, obs) => e.Observations = obs, e => e.Observations);

                case nameof(SaberPro):
                    return await PrepareStandardStageEntityAsync<SaberPro>(
                        entityId, e => e.IdStateStage, e => e.Id,
                        (e, obs) => e.Observations = obs, e => e.Observations);

                case nameof(ScientificArticle):
                    return await PrepareStandardStageEntityAsync<ScientificArticle>(
                        entityId, e => e.IdStateStage, e => e.Id,
                        (e, obs) => e.Observations = obs, e => e.Observations);

                default:
                    throw new InvalidOperationException($"El tipo de entidad '{entityType}' no soporta edición de observaciones.");
            }
        }

        private async Task<Func<string, int, Task>> PrepareStandardStageEntityAsync<TEntity>(
            int entityId,
            Func<TEntity, int> getStateStageId,
            Func<TEntity, int> getInscriptionId,
            Action<TEntity, string?> setObservation,
            Expression<Func<TEntity, object?>> observationProperty)
            where TEntity : BaseEntity<int>
        {
            var repo = _unitOfWork.GetRepository<TEntity, int>();
            var entity = await ValidateStageEntityOpenAsync(repo, entityId, getStateStageId, getInscriptionId);
            return async (obs, userId) =>
            {
                setObservation(entity, obs);
                entity.UpdatedAt = DateTimeOffset.UtcNow;
                entity.IdUserUpdatedAt = userId;
                await repo.UpdatePartialAsync(entity, [observationProperty, x => x.UpdatedAt, x => x.IdUserUpdatedAt]);
            };
        }

        private async Task<TEntity> ValidateStageEntityOpenAsync<TEntity>(
            IRepository<TEntity, int> repo,
            int entityId,
            Func<TEntity, int> getStateStageId,
            Func<TEntity, int> getInscriptionId)
            where TEntity : BaseEntity<int>
        {
            var entity = await repo.GetByIdAsync(entityId)
                ?? throw new KeyNotFoundException($"{typeof(TEntity).Name} with ID {entityId} not found.");

            var inscriptionId = getInscriptionId(entity);
            if (inscriptionId > 0)
            {
                var inscription = await _unitOfWork.GetRepository<InscriptionModality, int>().GetByIdAsync(inscriptionId);
                if (inscription?.IsFinalRejection == true)
                    throw new InvalidOperationException("No se puede editar la observación porque la modalidad ya tiene un rechazo definitivo.");
            }

            var stateStageId = getStateStageId(entity);
            var stateStage = await _unitOfWork.GetRepository<StateStage, int>().GetByIdAsync(stateStageId);
            if (stateStage != null && (stateStage.IsFinalStateForStage || stateStage.IsFinalStateForModalityOverall || stateStage.IsRejectionState))
            {
                throw new InvalidOperationException($"No se puede editar la observación porque el registro ya se encuentra en estado final '{stateStage.Name}'.");
            }

            return entity;
        }
    }
}

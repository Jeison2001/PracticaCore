using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.Common.Services
{
    /// <summary>
    /// Helper para registrar historial de observaciones en la tabla Evaluation
    /// cuando un evaluador/revisor actualiza las observaciones de una entidad.
    /// </summary>
    internal static class ObservationHistoryHelper
    {
        internal static async Task<bool> CreateIfChangedAsync(
            IUnitOfWork unitOfWork,
            string entityType,
            int entityId,
            int evaluatorId,
            string evaluationTypeCode,
            string? newObservations,
            string? previousObservations,
            CancellationToken cancellationToken = default,
            string? result = null,
            int? effectiveStateStageId = null)
        {
            if (evaluatorId <= 0)
                return false;

            var trimmedNew = newObservations?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedNew))
                return false;

            if (string.Equals(trimmedNew, previousObservations?.Trim(), StringComparison.Ordinal))
                return false;

            await CreateAsync(
                unitOfWork,
                entityType,
                entityId,
                evaluatorId,
                evaluationTypeCode,
                trimmedNew,
                cancellationToken,
                result,
                effectiveStateStageId);

            return true;
        }

        internal static async Task CreateAsync(
            IUnitOfWork unitOfWork,
            string entityType,
            int entityId,
            int evaluatorId,
            string evaluationTypeCode,
            string observations,
            CancellationToken cancellationToken = default,
            string? result = null,
            int? effectiveStateStageId = null)
        {
            if (evaluatorId <= 0)
                return;
            var evalTypeRepo = unitOfWork.GetRepository<EvaluationType, int>();
            if (evalTypeRepo == null)
                return;

            var evalType = await evalTypeRepo.GetFirstOrDefaultAsync(
                et => et.Code == evaluationTypeCode, cancellationToken);

            if (evalType == null)
                return;

            if (string.IsNullOrWhiteSpace(result) && effectiveStateStageId is > 0)
            {
                var stateRepo = unitOfWork.GetRepository<StateStage, int>();
                if (stateRepo != null)
                {
                    result = (await stateRepo.GetByIdAsync(effectiveStateStageId.Value))?.Code;
                }
            }

            var evalRepo = unitOfWork.GetRepository<Evaluation, int>();
            if (evalRepo == null)
                return;

            await evalRepo.AddAsync(new Evaluation
            {
                EntityType = entityType,
                EntityId = entityId,
                IdEvaluator = evaluatorId,
                IdEvaluationType = evalType.Id,
                Result = string.IsNullOrWhiteSpace(result) ? null : result.Trim(),
                Observations = observations
            });
        }
    }
}

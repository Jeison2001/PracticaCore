using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.Common.Services
{
    public static class ModalityStateHelper
    {
        public static Task ValidateStateTransitionAndCheckRejectionAsync(
            IUnitOfWork unitOfWork,
            int inscriptionModalityId,
            int originalStateId,
            int newStateId,
            CancellationToken cancellationToken = default) =>
            ValidateStateTransitionAndCheckRejectionAsync(
                unitOfWork,
                inscriptionModalityId,
                originalStateId,
                newStateId,
                allowCrossStageWithinSameModality: false,
                cancellationToken);

        public static async Task ValidateStateTransitionAndCheckRejectionAsync(
            IUnitOfWork unitOfWork,
            int inscriptionModalityId,
            int originalStateId,
            int newStateId,
            bool allowCrossStageWithinSameModality,
            CancellationToken cancellationToken = default)
        {
            var stateStageRepo = unitOfWork.GetRepository<StateStage, int>();
            var currentState = await stateStageRepo.GetByIdAsync(originalStateId);
            var newState = await stateStageRepo.GetByIdAsync(newStateId);

            if (currentState != null)
            {
                bool isModalityClosed = currentState.IsFinalStateForModalityOverall || currentState.IsRejectionState;
                bool isSameStageClosed = currentState.IsFinalStateForStage
                    && (newState == null || currentState.IdStageModality == newState.IdStageModality);

                if (isModalityClosed || isSameStageClosed)
                {
                    throw new InvalidOperationException($"No se puede modificar el estado de un registro que ya se encuentra en estado '{currentState.Name}'.");
                }
            }

            if (newState == null)
            {
                throw new InvalidOperationException("El estado destino especificado no existe.");
            }

            if (currentState != null &&
                currentState.IdStageModality > 0 &&
                newState.IdStageModality > 0 &&
                currentState.IdStageModality != newState.IdStageModality)
            {
                var stageRepo = unitOfWork.GetRepository<StageModality, int>();
                var currentStage = await stageRepo.GetByIdAsync(currentState.IdStageModality);
                var targetStage = await stageRepo.GetByIdAsync(newState.IdStageModality);

                if (currentStage != null && targetStage != null && currentStage.IdModality != targetStage.IdModality)
                {
                    throw new InvalidOperationException("No se puede transicionar hacia un estado perteneciente a otra modalidad.");
                }

                if (!allowCrossStageWithinSameModality)
                {
                    throw new InvalidOperationException("No se puede transicionar directamente a un estado perteneciente a otra etapa.");
                }
            }

            if (newState.IsRejectionState)
            {
                var inscriptionRepo = unitOfWork.GetRepository<InscriptionModality, int>();
                var inscription = await inscriptionRepo.GetByIdAsync(inscriptionModalityId);
                if (inscription != null && !inscription.IsFinalRejection)
                {
                    inscription.IsFinalRejection = true;
                    inscription.UpdatedAt = DateTimeOffset.UtcNow;
                    await inscriptionRepo.UpdatePartialAsync(inscription, [x => x.IsFinalRejection, x => x.UpdatedAt]);
                }
            }
        }
    }
}

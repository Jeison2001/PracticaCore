using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.Common.Services
{
    public static class ModalityStateHelper
    {
        public static async Task ValidateStateTransitionAndCheckRejectionAsync(
            IUnitOfWork unitOfWork,
            int inscriptionModalityId,
            int originalStateId,
            int newStateId,
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

            if (newState != null && newState.IsRejectionState)
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

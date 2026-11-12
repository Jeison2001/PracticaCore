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
            if (currentState != null && (currentState.IsFinalStateForStage || currentState.IsFinalStateForModalityOverall))
            {
                throw new InvalidOperationException($"No se puede modificar el estado de un registro que ya se encuentra en estado '{currentState.Name}'.");
            }

            var newState = await stateStageRepo.GetByIdAsync(newStateId);
            if (newState != null && (newState.IsRejectionState || ((newState.IsFinalStateForStage || newState.IsFinalStateForModalityOverall) && (newState.Code.Contains("RECHAZ") || newState.Code.Contains("NO_APROBAD") || newState.Code.Contains("NO_PERTINENTE") || newState.Code.Contains("REPROBAD")))))
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

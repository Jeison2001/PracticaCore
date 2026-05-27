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
        internal static async Task CreateAsync(
            IUnitOfWork unitOfWork,
            string entityType,
            int entityId,
            int evaluatorId,
            string evaluationTypeCode,
            string observations,
            CancellationToken cancellationToken = default)
        {
            var evalTypeRepo = unitOfWork.GetRepository<EvaluationType, int>();
            var evalType = await evalTypeRepo.GetFirstOrDefaultAsync(
                et => et.Code == evaluationTypeCode, cancellationToken);

            if (evalType == null)
                return;

            var evalRepo = unitOfWork.GetRepository<Evaluation, int>();
            await evalRepo.AddAsync(new Evaluation
            {
                EntityType = entityType,
                EntityId = entityId,
                IdEvaluator = evaluatorId,
                IdEvaluationType = evalType.Id,
                Observations = observations
            });
        }
    }
}

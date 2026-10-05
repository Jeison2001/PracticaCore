using Application.Shared.DTOs.Evaluations;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Features.Evaluations.Queries.Handlers
{
    /// <summary>
    /// Devuelve el historial de evaluaciones/observaciones de una entidad ordenado por
    /// fecha descendente, resuelviendo el nombre del evaluador y el tipo de evaluación
    /// en consultas por lotes (sin N+1).
    /// </summary>
    public class GetObservationHistoryByEntityHandler
        : IRequestHandler<GetObservationHistoryByEntityQuery, List<ObservationHistoryItemDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetObservationHistoryByEntityHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<ObservationHistoryItemDto>> Handle(
            GetObservationHistoryByEntityQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.EntityType))
                return [];

            var evalRepo = _unitOfWork.GetRepository<Evaluation, int>();
            var evaluations = (await evalRepo.GetAllAsync(
                e => e.EntityType == request.EntityType
                     && e.EntityId == request.EntityId
                     && (!request.IdEvaluationType.HasValue || e.IdEvaluationType == request.IdEvaluationType.Value),
                q => q.OrderByDescending(e => e.CreatedAt))).ToList();

            if (evaluations.Count == 0)
                return [];

            var userIds = evaluations.Select(e => e.IdEvaluator).Distinct().ToList();
            var typeIds = evaluations.Select(e => e.IdEvaluationType).Distinct().ToList();

            var userRepo = _unitOfWork.GetRepository<User, int>();
            var users = (await userRepo.GetAllAsync(u => userIds.Contains(u.Id)))
                .ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim());

            var typeRepo = _unitOfWork.GetRepository<EvaluationType, int>();
            var types = (await typeRepo.GetAllAsync(t => typeIds.Contains(t.Id)))
                .ToDictionary(t => t.Id, t => (Code: t.Code, Name: t.Name));

            return evaluations.Select(e => new ObservationHistoryItemDto
            {
                Id = e.Id,
                EntityId = e.EntityId,
                IdEvaluator = e.IdEvaluator,
                EvaluatorName = users.TryGetValue(e.IdEvaluator, out var name) ? name : null,
                IdEvaluationType = e.IdEvaluationType,
                EvaluationTypeCode = types.TryGetValue(e.IdEvaluationType, out var tinfo) ? tinfo.Code : null,
                EvaluationTypeName = types.TryGetValue(e.IdEvaluationType, out var tinfoName) ? tinfoName.Name : null,
                Result = e.Result,
                Observations = e.Observations,
                CreatedAt = e.CreatedAt
            }).ToList();
        }
    }
}

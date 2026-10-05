using Application.Shared.DTOs.Evaluations;
using MediatR;

namespace Application.Features.Evaluations.Queries
{
    /// <summary>
    /// Historial de observaciones/retroalimentaciones de una entidad (mitad de lectura
    /// de la tabla transversal Evaluation, que ya se escribe desde los PATCH de calificación).
    /// </summary>
    public record GetObservationHistoryByEntityQuery(
        string EntityType,
        int EntityId,
        int? IdEvaluationType = null) : IRequest<List<ObservationHistoryItemDto>>;
}

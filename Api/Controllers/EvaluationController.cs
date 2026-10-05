using Application.Features.Evaluations.Queries;
using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Evaluations;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class EvaluationController : GenericController<Evaluation, int, EvaluationDto>
    {
        public EvaluationController(IMediator mediator) : base(mediator)
        {
        }

        /// <summary>
        /// Historial de observaciones/retroalimentaciones de una entidad
        /// (tabla transversal Evaluation), ordenado por fecha descendente.
        /// </summary>
        [HttpGet("history/{entityType}/{entityId}")]
        public async Task<IActionResult> GetObservationHistory(
            string entityType, int entityId, [FromQuery] int? idEvaluationType)
        {
            var query = new GetObservationHistoryByEntityQuery(entityType, entityId, idEvaluationType);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<ObservationHistoryItemDto>> { Success = true, Data = result });
        }
    }
}
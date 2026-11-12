using Api.Responses;
using Application.Features.Evaluations.Commands;
using Application.Features.Evaluations.Queries;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Evaluations;
using Domain.Common;
using Domain.Common.Extensions;
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

        [NonAction]
        public override Task<IActionResult> GetById(int id) => base.GetById(id);

        [NonAction]
        public override Task<IActionResult> GetAll([FromQuery] PaginatedRequest request) => base.GetAll(request);

        [NonAction]
        public override Task<IActionResult> Create([FromBody] EvaluationDto dto) => base.Create(dto);

        [NonAction]
        public override Task<IActionResult> CreateMultiple([FromBody] List<EvaluationDto> dtos) => base.CreateMultiple(dtos);

        [NonAction]
        public override Task<IActionResult> Update(int id, [FromBody] EvaluationDto dto) => base.Update(id, dto);

        [NonAction]
        public override Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto) => base.UpdateStatus(id, dto);

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

        /// <summary>
        /// Permite al propio autor editar el texto de su observación mientras la etapa o modalidad siga abierta.
        /// </summary>
        [HttpPatch("{id}/observations")]
        public async Task<IActionResult> UpdateObservation(
            int id, [FromBody] UpdateEvaluationObservationDto dto)
        {
            var command = new UpdateEvaluationObservationCommand(id, dto, User.GetCurrentUserInfo());
            var result = await _mediator.Send(command);
            return Ok(new ApiResponse<ObservationHistoryItemDto> { Success = true, Data = result });
        }
    }
}
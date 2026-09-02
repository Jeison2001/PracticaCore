using Api.Responses;
using Application.Shared.Commands.TeacherEnabledCargos;
using Application.Shared.DTOs.TeacherEnabledCargos;
using Application.Shared.Queries.TeacherEnabledCargos;
using Domain.Common.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    /// <summary>
    /// Cargos habilitados por docente (TeacherEnabledCargo).
    /// GET /ByUser/{id}: cargos activos del docente.
    /// POST /ByUser/{id}: recibe el array de ids de cargos y reconcilia (upsert):
    /// activa los recibidos y desactiva los que ya no están en la lista.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TeacherEnabledCargoController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TeacherEnabledCargoController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Obtiene los cargos habilitados (activos) de un docente.
        /// </summary>
        /// <param name="id">ID del docente (User)</param>
        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetEnabledCargosByUser(int id)
        {
            if (id <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Errors = new List<string> { "El ID de usuario debe ser válido." } });

            var result = await _mediator.Send(new GetTeacherEnabledCargosQuery { UserId = id });
            return Ok(new ApiResponse<List<TeacherEnabledCargoDto>> { Success = true, Data = result });
        }

        /// <summary>
        /// Asigna los cargos habilitados a un docente (lista completa: upsert semántico).
        /// </summary>
        /// <param name="request">UserId del docente y array de ids de TypeTeachingAssignment habilitados</param>
        [HttpPost("ByUser")]
        public async Task<IActionResult> AssignEnabledCargos([FromBody] AssignTeacherEnabledCargosRequestDto request)
        {
            if (request == null || request.UserId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Errors = new List<string> { "El ID de usuario debe ser válido." } });

            var result = await _mediator.Send(new AssignTeacherEnabledCargosCommand
            {
                UserId = request.UserId,
                TypeTeachingAssignmentIds = request.TypeTeachingAssignmentIds ?? new List<int>(),
                CurrentUser = User.GetCurrentUserInfo()
            });

            return Ok(new ApiResponse<List<TeacherEnabledCargoDto>> { Success = true, Data = result });
        }
    }
}

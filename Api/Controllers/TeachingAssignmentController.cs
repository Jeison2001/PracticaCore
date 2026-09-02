using Api.Responses;
using Application.Shared.Commands.TeachingAssignments;
using Application.Shared.DTOs.TeachingAssignments;
using Application.Shared.Queries.TeachingAssignments;
using Domain.Common.Extensions;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    /// <summary>
    /// Controlador de Asignaciones Docentes. Override Create/Update del GenericController
    /// para usar CreateTeachingAssignmentCommand (valida MaxAssignments por docente)
    /// y UpdateTeachingAssignmentCommand (notifica al docente anterior en reassignments).
    /// GetByInscription retorna docentes asignados a una inscripción.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TeachingAssignmentController : GenericController<TeachingAssignment, int, TeachingAssignmentDto>
    {
        private readonly IMediator _mediator;
        public TeachingAssignmentController(IMediator mediator) : base(mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("ByInscription/{id}")]
        [ProducesResponseType(typeof(ApiResponse<List<TeachingAssignmentTeacherDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByIdInscription(int id, bool? status = null)
        {
            var result = await _mediator.Send(new GetTeachingAssignmentsByProposalIdQuery(id, status));
            return Ok(new ApiResponse<List<TeachingAssignmentTeacherDto>> { Success = true, Data = result });
        }

        /// <summary>
        /// Obtiene las asignaciones y proyectos de un docente categorizados por cargo docente (Director, Co-Director, Asesor, Jurado).
        /// </summary>
        /// <param name="idTeacher">ID del docente (User)</param>
        /// <param name="idCargo">ID opcional del cargo para filtrar únicamente ese cargo</param>
        /// <param name="includeRevoked">Indica si se deben incluir asignaciones revocadas (por defecto false)</param>
        [HttpGet("ByTeacher/{idTeacher}")]
        [ProducesResponseType(typeof(ApiResponse<List<TeacherCargoGroupDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssignmentsByTeacher(
            int idTeacher,
            [FromQuery] int? idCargo = null,
            [FromQuery] bool includeRevoked = false)
        {
            if (idTeacher <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Errors = new List<string> { "El ID de docente debe ser válido." } });

            var query = new GetTeachingAssignmentsByTeacherQuery(idTeacher, idCargo, includeRevoked);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<TeacherCargoGroupDto>> { Success = true, Data = result });
        }

        /// <summary>
        /// Obtiene las asignaciones y proyectos de un docente filtrados por un cargo específico.
        /// </summary>
        /// <param name="idTeacher">ID del docente (User)</param>
        /// <param name="idCargo">ID del cargo docente (TypeTeachingAssignment)</param>
        /// <param name="includeRevoked">Indica si se deben incluir asignaciones revocadas (por defecto false)</param>
        [HttpGet("ByTeacherAndCargo/{idTeacher}/{idCargo}")]
        [ProducesResponseType(typeof(ApiResponse<List<TeacherAssignedProjectDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssignmentsByTeacherAndCargo(
            int idTeacher,
            int idCargo,
            [FromQuery] bool includeRevoked = false)
        {
            if (idTeacher <= 0 || idCargo <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Errors = new List<string> { "Los IDs de docente y cargo deben ser válidos." } });

            var query = new GetTeachingAssignmentsByTeacherAndCargoQuery(idTeacher, idCargo, includeRevoked);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<TeacherAssignedProjectDto>> { Success = true, Data = result });
        }
        
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] TeachingAssignmentDto dto)
        {
            var result = await _mediator.Send(new CreateTeachingAssignmentCommand(dto, User.GetCurrentUserInfo()));
            return CreatedAtAction(nameof(GetByIdInscription), new { id = result.IdInscriptionModality }, new ApiResponse<TeachingAssignmentDto> { Success = true, Data = result });
        }

        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] TeachingAssignmentDto dto)
        {
            var result = await _mediator.Send(new UpdateTeachingAssignmentCommand(id, dto, User.GetCurrentUserInfo()));
            return Ok(new ApiResponse<TeachingAssignmentDto> { Success = true, Data = result });
        }
    }
}
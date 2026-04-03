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
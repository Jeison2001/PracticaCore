using Application.Shared.Commands.TeachingAssignment;
using Application.Shared.DTOs.TeachingAssignment;
using Application.Shared.Queries.TeachingAssignment;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
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
        [ProducesResponseType(typeof(List<TeachingAssignmentTeacherDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByIdInscription(int id, bool? status = null)
        {
            var result = await _mediator.Send(new GetTeachingAssignmentsByProposalIdQuery(id, status));
            return Ok(result);
        }
        
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] TeachingAssignmentDto dto)
        {
            try
            {
                var result = await _mediator.Send(new CreateTeachingAssignmentCommand(dto));
                return CreatedAtAction(nameof(GetByIdInscription), new { id = result.IdInscriptionModality }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] TeachingAssignmentDto dto)
        {
            try
            {
                var result = await _mediator.Send(new UpdateTeachingAssignmentCommand(id, dto));
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
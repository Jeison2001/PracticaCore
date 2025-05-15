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
    }
}
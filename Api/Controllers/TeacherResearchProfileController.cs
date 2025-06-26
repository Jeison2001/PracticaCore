using Application.Shared.Commands.TeacherResearchProfile;
using Application.Shared.DTOs.TeacherResearchProfile;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Domain.Entities;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeacherResearchProfileController : GenericController<TeacherResearchProfile, int, TeacherResearchProfileDto>
    {
        private readonly IMediator _mediator;
        public TeacherResearchProfileController(IMediator mediator) : base(mediator)
        {
            _mediator = mediator;
        }

        [NonAction]
        public override Task<IActionResult> GetById(int id) => Task.FromResult<IActionResult>(NotFound());

        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] TeacherResearchProfileDto dto)
        {
            try
            {
                var result = await _mediator.Send(new CreateTeacherResearchProfileCommand(dto));
                return CreatedAtAction(nameof(GetAll), new { idUser = result.IdUser }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] TeacherResearchProfileDto dto)
        {
            try
            {
                var result = await _mediator.Send(new UpdateTeacherResearchProfileCommand(id, dto));
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

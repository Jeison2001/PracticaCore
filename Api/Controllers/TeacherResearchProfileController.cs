using Api.Responses;
using Application.Shared.Commands.TeacherResearchProfiles;
using Application.Shared.DTOs.TeacherResearchProfiles;
using Domain.Common.Extensions;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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
            var result = await _mediator.Send(new CreateTeacherResearchProfileCommand(dto, User.GetCurrentUserInfo()));
            return CreatedAtAction(nameof(GetAll), new { idUser = result.IdUser }, new ApiResponse<TeacherResearchProfileDto> { Success = true, Data = result });
        }

        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] TeacherResearchProfileDto dto)
        {
            var result = await _mediator.Send(new UpdateTeacherResearchProfileCommand(id, dto, User.GetCurrentUserInfo()));
            return Ok(new ApiResponse<TeacherResearchProfileDto> { Success = true, Data = result });
        }
    }
}

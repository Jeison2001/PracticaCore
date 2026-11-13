using Api.Authorization;
using Api.Responses;
using Application.Shared.Commands.TeacherResearchProfiles;
using Application.Shared.DTOs;
using Application.Shared.DTOs.TeacherResearchProfiles;
using Domain.Common;
using Domain.Common.Extensions;
using Domain.Constants;
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

        [RequirePermission(
            PermissionCodes.Configuraciones.N3CFSU,
            PermissionCodes.Configuraciones.N3CFING,
            PermissionCodes.Configuraciones.N3CFINL,
            PermissionCodes.Configuraciones.N3CFINS)]
        [HttpGet]
        public override async Task<IActionResult> GetAll([FromQuery] PaginatedRequest request) => await base.GetAll(request);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSU)]
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] TeacherResearchProfileDto dto)
        {
            var result = await _mediator.Send(new CreateTeacherResearchProfileCommand(dto, User.GetCurrentUserInfo()));
            return CreatedAtAction(nameof(GetAll), new { idUser = result.IdUser }, new ApiResponse<TeacherResearchProfileDto> { Success = true, Data = result });
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSU)]
        [HttpPost("multiple")]
        public override async Task<IActionResult> CreateMultiple([FromBody] List<TeacherResearchProfileDto> dtos) => await base.CreateMultiple(dtos);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSU)]
        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] TeacherResearchProfileDto dto)
        {
            var result = await _mediator.Send(new UpdateTeacherResearchProfileCommand(id, dto, User.GetCurrentUserInfo()));
            return Ok(new ApiResponse<TeacherResearchProfileDto> { Success = true, Data = result });
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSU)]
        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto) => await base.UpdateStatus(id, dto);
    }
}

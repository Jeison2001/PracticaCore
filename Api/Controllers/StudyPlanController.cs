using Api.Authorization;
using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.StudyPlans;
using Application.Shared.Queries.StudyPlans.GetStudyPlansByAcademicProgram;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    public class StudyPlanController : GenericController<StudyPlan, int, StudyPlanDto>
    {
        public StudyPlanController(IMediator mediator) : base(mediator)
        {
        }

        [HttpGet("ByAcademicProgram/{academicProgramId}")]
        public async Task<ActionResult<ApiResponse<List<StudyPlanDto>>>> GetByAcademicProgram(int academicProgramId, CancellationToken cancellationToken)
        {
            var query = new GetStudyPlansByAcademicProgramQuery(academicProgramId);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(new ApiResponse<List<StudyPlanDto>> { Success = true, Data = result, Errors = [], Messages = [] });
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFACPE)]
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] StudyPlanDto dto) => await base.Create(dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFACPE)]
        [HttpPost("multiple")]
        public override async Task<IActionResult> CreateMultiple([FromBody] List<StudyPlanDto> dtos) => await base.CreateMultiple(dtos);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFACPE)]
        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] StudyPlanDto dto) => await base.Update(id, dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFACPE)]
        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto) => await base.UpdateStatus(id, dto);
    }
}

using Application.Shared.DTOs.StudyPlans;
using Application.Shared.Queries.StudyPlans.GetStudyPlansByAcademicProgram;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Api.Responses;

namespace Api.Controllers
{
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
    }
}

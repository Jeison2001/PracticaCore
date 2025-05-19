using Api.Controllers;
using Application.Shared.DTOs;
using Application.Shared.DTOs.ProjectFinal;
using Application.Shared.Queries.ProjectFinal;
using Microsoft.AspNetCore.Mvc;
using Api.Responses;
using Domain.Common;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class ProjectFinalController : GenericController<ProjectFinal, int, ProjectFinalDto>
    {
        private readonly IMediator _mediator;
        public ProjectFinalController(IMediator mediator) : base(mediator) { _mediator = mediator; }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllWithDetails([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllProjectFinalsQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                IsDescending = request.IsDescending,
                Filters = request.Filters
            };
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<ProjectFinalWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            var query = new GetProjectFinalsByUserQuery(id, status);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<ProjectFinalWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [HttpGet("ByTeacher/{id}")]
        public async Task<IActionResult> GetByTeacherId(int id, [FromQuery] PaginatedRequest request)
        {
            var query = new GetProjectFinalsByTeacherQuery(
                id,
                request.PageNumber,
                request.PageSize,
                request.SortBy ?? string.Empty,
                request.IsDescending,
                request.Filters ?? new Dictionary<string, string>()
            );
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<ProjectFinalWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [NonAction]
        public override Task<IActionResult> GetAll([FromQuery] PaginatedRequest request)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> GetById(int id)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }
    }
}

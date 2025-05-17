using Api.Controllers;
using Application.Shared.DTOs.PreliminaryProject;
using Application.Shared.Queries.PreliminaryProject;
using Application.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Api.Responses;
using Domain.Common;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class PreliminaryProjectController : GenericController<PreliminaryProject, int, PreliminaryProjectDto>
    {
        private readonly IMediator _mediator;
        public PreliminaryProjectController(IMediator mediator) : base(mediator) { _mediator = mediator; }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllWithDetails([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllPreliminaryProjectsQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                IsDescending = request.IsDescending,
                Filters = request.Filters
            };
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<PreliminaryProjectWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            var query = new GetPreliminaryProjectsByUserQuery(id, status);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<PreliminaryProjectWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [HttpGet("ByTeacher/{id}")]
        public async Task<IActionResult> GetByTeacherId(int id, [FromQuery] PaginatedRequest request)
        {
            var query = new GetPreliminaryProjectsByTeacherQuery(
                id,
                request.PageNumber,
                request.PageSize,
                request.SortBy ?? string.Empty,
                request.IsDescending,
                request.Filters ?? new Dictionary<string, string>()
            );
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<PreliminaryProjectWithDetailsResponseDto>> { Success = true, Data = result });
        }
    }
}

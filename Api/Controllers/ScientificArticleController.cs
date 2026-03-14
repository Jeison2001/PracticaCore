using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.ScientificArticles;
using Application.Shared.Queries.ScientificArticles;
using Domain.Entities;
using Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class ScientificArticleController : GenericController<ScientificArticle, int, ScientificArticleDto>
    {
        private readonly IMediator _mediator;

        public ScientificArticleController(IMediator mediator) : base(mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllWithDetails([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllScientificArticlesQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy ?? string.Empty,
                IsDescending = request.IsDescending,
                Filters = request.Filters ?? new Dictionary<string, string>()
            };

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<ScientificArticleWithDetailsDto>> { Success = true, Data = result });
        }

        [HttpGet("WithDetails/{id}")]
        public async Task<IActionResult> GetWithDetails(int id)
        {
            var result = await _mediator.Send(new GetScientificArticleWithDetailsQuery(id));
            return Ok(new ApiResponse<ScientificArticleWithDetailsDto> { Success = true, Data = result });
        }

        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            var query = new GetScientificArticlesByUserQuery(id, status);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<ScientificArticleWithDetailsDto>> { Success = true, Data = result });
        }

        [HttpGet("ByTeacher/{id}")]
        public async Task<IActionResult> GetByTeacherId(int id, [FromQuery] PaginatedRequest request)
        {
            var query = new GetScientificArticlesByTeacherQuery(
                id,
                request.PageNumber,
                request.PageSize,
                request.SortBy ?? string.Empty,
                request.IsDescending,
                request.Filters ?? new Dictionary<string, string>()
            );

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<ScientificArticleWithDetailsDto>> { Success = true, Data = result });
        }

        [NonAction]
        public override Task<IActionResult> GetById(int id)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }
    }
}
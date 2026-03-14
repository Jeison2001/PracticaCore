using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Seminars;
using Application.Shared.Queries.Seminars;
using Domain.Entities;
using Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class SeminarController : GenericController<Seminar, int, SeminarDto>
    {
        private readonly IMediator _mediator;

        public SeminarController(IMediator mediator) : base(mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllWithDetails([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllSeminarsQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy ?? string.Empty,
                IsDescending = request.IsDescending,
                Filters = request.Filters ?? new Dictionary<string, string>()
            };

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<SeminarWithDetailsDto>> { Success = true, Data = result });
        }

        [HttpGet("WithDetails/{id}")]
        public async Task<IActionResult> GetWithDetails(int id)
        {
            var result = await _mediator.Send(new GetSeminarWithDetailsQuery(id));
            return Ok(new ApiResponse<SeminarWithDetailsDto> { Success = true, Data = result });
        }

        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            var query = new GetSeminarsByUserQuery(id, status);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<SeminarWithDetailsDto>> { Success = true, Data = result });
        }

        [HttpGet("ByTeacher/{id}")]
        public async Task<IActionResult> GetByTeacherId(int id, [FromQuery] PaginatedRequest request)
        {
            var query = new GetSeminarsByTeacherQuery(
                id,
                request.PageNumber,
                request.PageSize,
                request.SortBy ?? string.Empty,
                request.IsDescending,
                request.Filters ?? new Dictionary<string, string>()
            );

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<SeminarWithDetailsDto>> { Success = true, Data = result });
        }

        [NonAction]
        public override Task<IActionResult> GetAll(PaginatedRequest request)
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

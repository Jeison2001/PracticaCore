using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.AcademicAverages;
using Application.Shared.Queries.AcademicAverages;
using Domain.Entities;
using Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class AcademicAverageController : GenericController<AcademicAverage, int, AcademicAverageDto>
    {
        private readonly IMediator _mediator;

        public AcademicAverageController(IMediator mediator) : base(mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllWithDetails([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllAcademicAveragesQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy ?? string.Empty,
                IsDescending = request.IsDescending,
                Filters = request.Filters ?? new Dictionary<string, string>()
            };

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<AcademicAverageWithDetailsDto>> { Success = true, Data = result });
        }

        [HttpGet("WithDetails/{id}")]
        public async Task<IActionResult> GetWithDetails(int id)
        {
            var result = await _mediator.Send(new GetAcademicAverageWithDetailsQuery(id));
            return Ok(new ApiResponse<AcademicAverageWithDetailsDto> { Success = true, Data = result });
        }

        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            var query = new GetAcademicAveragesByUserQuery(id, status);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<AcademicAverageWithDetailsDto>> { Success = true, Data = result });
        }

        [HttpGet("ByTeacher/{id}")]
        public async Task<IActionResult> GetByTeacherId(
            int id,
            [FromQuery] PaginatedRequest request)
        {
            var query = new GetAcademicAveragesByTeacherQuery(
                id,
                request.PageNumber,
                request.PageSize,
                request.SortBy ?? string.Empty,
                request.IsDescending,
                request.Filters ?? new Dictionary<string, string>()
            );

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<AcademicAverageWithDetailsDto>> { Success = true, Data = result });
        }

        [NonAction]
        public override Task<IActionResult> GetById(int id)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }
    }
}

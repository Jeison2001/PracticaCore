using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.CoTerminals;
using Application.Shared.Queries.CoTerminals;
using Domain.Entities;
using Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class CoTerminalController : GenericController<CoTerminal, int, CoTerminalDto>
    {
        private readonly IMediator _mediator;

        public CoTerminalController(IMediator mediator) : base(mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllWithDetails([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllCoTerminalsQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy ?? string.Empty,
                IsDescending = request.IsDescending,
                Filters = request.Filters ?? new Dictionary<string, string>()
            };

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<CoTerminalWithDetailsDto>> { Success = true, Data = result });
        }

        [HttpGet("WithDetails/{id}")]
        public async Task<IActionResult> GetWithDetails(int id)
        {
            var result = await _mediator.Send(new GetCoTerminalWithDetailsQuery(id));
            return Ok(new ApiResponse<CoTerminalWithDetailsDto> { Success = true, Data = result });
        }

        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            var query = new GetCoTerminalsByUserQuery(id, status);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<CoTerminalWithDetailsDto>> { Success = true, Data = result });
        }

        [HttpGet("ByTeacher/{id}")]
        public async Task<IActionResult> GetByTeacherId(
            int id,
            [FromQuery] PaginatedRequest request)
        {
            var query = new GetCoTerminalsByTeacherQuery(
                id,
                request.PageNumber,
                request.PageSize,
                request.SortBy ?? string.Empty,
                request.IsDescending,
                request.Filters ?? new Dictionary<string, string>()
            );

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<CoTerminalWithDetailsDto>> { Success = true, Data = result });
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

using Application.Shared.Queries;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Application.Shared.Commands;
using Api.Responses;
using Application.Shared.DTOs;
using Domain.Common;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenericController<T, TId, TDto> : ControllerBase
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>
    {
        private readonly IMediator _mediator;

        public GenericController(IMediator mediator) => _mediator = mediator;

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(TId id)
        {
            var result = await _mediator.Send(new GetEntityByIdQuery<T, TId, TDto>(id));
            return Ok(new ApiResponse<TDto> { Success = true, Data = result });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllEntitiesQuery<T, TId, TDto>
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                IsDescending = request.IsDescending,
                Filters = request.Filters
            };

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<TDto>> { Success = true, Data = result });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TDto dto)
        {
            var result = await _mediator.Send(new CreateEntityCommand<T, TId, TDto>(dto));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<TDto> { Success = true, Data = result });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(TId id, [FromBody] TDto dto)
        {
            var result = await _mediator.Send(new UpdateEntityCommand<T, TId, TDto>(id, dto));
            return Ok(new ApiResponse<TDto> { Success = true, Data = result });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(TId id, [FromBody] UpdateStatusRequestDto dto)
        {
            var command = new UpdateStatusEntityCommand<T, TId>(
                id,
                dto.StatusRegister,
                dto.IdUserUpdateAt,
                dto.OperationRegister
            );

            var result = await _mediator.Send(command);
            return result
                ? Ok(new ApiResponse<object> { Success = true, Messages = new List<string> { "Entity status updated successfully" } })
                : NotFound(new ApiResponse<object> { Success = false, Errors = new List<string> { "Entity not found or update failed" } });
        }
    }
}

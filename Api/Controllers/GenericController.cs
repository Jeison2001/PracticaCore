using Application.Shared.Queries;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Application.Shared.Commands;
using Api.Responses;
using Application.Shared.DTOs;

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
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetAllEntitiesQuery<T, TId, TDto>());
            return Ok(new ApiResponse<IEnumerable<TDto>> { Success = true, Data = result });
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(TId id)
        {
            var result = await _mediator.Send(new DeleteEntityCommand<T, TId>(id));
            return result
                ? Ok(new ApiResponse<object> { Success = true, Messages = new List<string> { "Entity deleted successfully" } })
                : NotFound(new ApiResponse<object> { Success = false, Errors = new List<string> { "Entity not found" } });
        }
    }
}

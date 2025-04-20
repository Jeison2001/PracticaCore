using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Responses;
using Application.Shared.Commands.RegisterModalityWithStudents;
using Application.Shared.DTOs;
using Application.Shared.DTOs.RegisterModalityWithStudents;
using Application.Shared.Queries.RegisterModalityWithStudents;
using Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegisterModalityWithStudentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RegisterModalityWithStudentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginatedRequest request)
        {
            try
            {
                var query = new GetAllRegisterModalityWithStudentsQuery
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    SortBy = request.SortBy,
                    IsDescending = request.IsDescending,
                    Filters = request.Filters
                };

                var result = await _mediator.Send(query);
                return Ok(new ApiResponse<PaginatedResult<RegisterModalityWithStudentsResponseDto>> { Success = true, Data = result });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"Error al obtener los registros de modalidad: {ex.Message}" }
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _mediator.Send(new GetRegisterModalityWithStudentsQuery(id));
                return Ok(new ApiResponse<RegisterModalityWithStudentsResponseDto> { Success = true, Data = result });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"No se encontró el registro de modalidad con ID {id}" }
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RegisterModalityWithStudentsCreateDto dto)
        {
            var result = await _mediator.Send(new CreateRegisterModalityWithStudentsCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = result.RegisterModality.Id },
                new ApiResponse<RegisterModalityWithStudentsDto> { Success = true, Data = result });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RegisterModalityWithStudentsUpdateDto dto)
        {
            try
            {
                var result = await _mediator.Send(new UpdateRegisterModalityWithStudentsCommand(id, dto));
                return Ok(new ApiResponse<RegisterModalityWithStudentsDto> { Success = true, Data = result });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"No se encontró el registro de modalidad con ID {id}" }
                });
            }
        }
    }
}
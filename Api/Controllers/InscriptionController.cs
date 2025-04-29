using Api.Responses;
using Application.Shared.Commands.InscriptionWithStudents;
using Application.Shared.DTOs;
using Application.Shared.DTOs.InscriptionWithStudents;
using Application.Shared.Queries.InscriptionWithStudents;
using Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InscriptionController : ControllerBase
    {
        private readonly IMediator _mediator;

        public InscriptionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginatedRequest request)
        {
            try
            {
                var query = new GetAllInscriptionWithStudentsQuery
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    SortBy = request.SortBy,
                    IsDescending = request.IsDescending,
                    Filters = request.Filters
                };

                var result = await _mediator.Send(query);
                return Ok(new ApiResponse<PaginatedResult<InscriptionWithStudentsResponseDto>> { Success = true, Data = result });
            }
            catch (Exception ex)
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
                var result = await _mediator.Send(new GetInscriptionWithStudentsQuery(id));
                return Ok(new ApiResponse<InscriptionWithStudentsResponseDto> { Success = true, Data = result });
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

        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            try
            {
                var query = new GetInscriptionWithStudentsByUserQuery(id, status);
                var result = await _mediator.Send(query);
                return Ok(new ApiResponse<List<InscriptionWithStudentsResponseDto>> { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"Error al obtener los registros de modalidad del usuario: {ex.Message}" }
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InscriptionWithStudentsCreateDto dto)
        {
            var result = await _mediator.Send(new CreateInscriptionWithStudentsCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = result.InscriptionModality.Id },
                new ApiResponse<InscriptionWithStudentsDto> { Success = true, Data = result });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] InscriptionWithStudentsUpdateDto dto)
        {
            try
            {
                var result = await _mediator.Send(new UpdateInscriptionWithStudentsCommand(id, dto));
                return Ok(new ApiResponse<InscriptionWithStudentsDto> { Success = true, Data = result });
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
using Api.Responses;
using Application.Shared.Commands.InscriptionWithStudents;
using Application.Shared.DTOs;
using Application.Shared.DTOs.InscriptionWithStudents;
using Application.Shared.Queries.InscriptionWithStudents;
using Domain.Common;
using Domain.Common.Extensions;
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetInscriptionWithStudentsQuery(id));
            return Ok(new ApiResponse<InscriptionWithStudentsResponseDto> { Success = true, Data = result });
        }

        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            var query = new GetInscriptionWithStudentsByUserQuery(id, status);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<InscriptionWithStudentsResponseDto>> { Success = true, Data = result });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InscriptionWithStudentsCreateDto dto)
        {
            var currentUser = User.GetCurrentUserInfo();
            var result = await _mediator.Send(new CreateInscriptionWithStudentsCommand(dto, currentUser));
            return CreatedAtAction(nameof(GetById), new { id = result.InscriptionModality.Id },
                new ApiResponse<InscriptionWithStudentsDto> { Success = true, Data = result });
        }

    }
}
using Api.Responses;
using Application.Shared.Commands.AcademicPractices;
using Application.Shared.DTOs;
using Application.Shared.DTOs.AcademicPractices;
using Application.Shared.Queries.AcademicPractices;
using Domain.Common;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class AcademicPracticeController : GenericController<AcademicPractice, int, AcademicPracticeDto>
    {
        private readonly IMediator _mediator;

        public AcademicPracticeController(IMediator mediator) : base(mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllWithDetails([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllAcademicPracticesQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy ?? string.Empty,
                IsDescending = request.IsDescending,
                Filters = request.Filters ?? new Dictionary<string, string>()
            };

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [HttpGet("WithDetails/{id}")]
        public async Task<IActionResult> GetWithDetails(int id)
        {
            var query = new GetAcademicPracticeWithDetailsQuery(id);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<AcademicPracticeWithDetailsResponseDto> { Success = true, Data = result });
        }

        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            var query = new GetAcademicPracticesByUserQuery(id, status);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<AcademicPracticeWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [HttpGet("ByTeacher/{id}")]
        public async Task<IActionResult> GetByTeacherId(
            int id,
            [FromQuery] PaginatedRequest request)
        {
            var query = new GetAcademicPracticesByTeacherQuery(
                id,
                request.PageNumber,
                request.PageSize,
                request.SortBy ?? string.Empty,
                request.IsDescending,
                request.Filters ?? new Dictionary<string, string>()
            );

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [HttpPut("UpdateInstitutionInfo/{id}")]
        public async Task<IActionResult> UpdateInstitutionInfo(int id, [FromBody] UpdateInstitutionInfoDto request)
        {
            request.Id = id;
            var result = await _mediator.Send(new UpdateAcademicPracticeInstitutionCommand(request));
            
            if (!result)
                return NotFound(new ApiResponse<object> { Success = false, Errors = new List<string> { $"No se encontró la práctica académica con ID {id}" } });

            return Ok(new ApiResponse<object> { Success = true, Messages = new List<string> { "Información de institución actualizada correctamente" } });
        }

        [HttpPut("UpdatePhaseApproval/{id}")]
        public async Task<IActionResult> UpdatePhaseApproval(int id, [FromBody] UpdatePhaseApprovalDto request)
        {
            request.Id = id;
            var result = await _mediator.Send(new UpdateAcademicPracticePhaseCommand(request));
            
            if (!result)
                return NotFound(new ApiResponse<object> { Success = false, Errors = new List<string> { $"No se encontró la práctica académica con ID {id} o el estado es inválido" } });

            return Ok(new ApiResponse<object> { Success = true, Messages = new List<string> { "Aprobación de fase actualizada correctamente" } });
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

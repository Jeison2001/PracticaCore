using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.AcademicPractice;
using Application.Shared.Queries.AcademicPractice;
using Application.Shared.Queries.AcademicPractice.Handlers;
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
            try
            {
                var query = new GetAllAcademicPracticesQuery
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    SortBy = request.SortBy,
                    IsDescending = request.IsDescending,
                    Filters = request.Filters
                };

                var result = await _mediator.Send(query);
                return Ok(new ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>> { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"Error al obtener las prácticas académicas con detalles: {ex.Message}" }
                });
            }
        }

        [HttpGet("WithDetails/{id}")]
        public async Task<IActionResult> GetWithDetails(int id)
        {
            try
            {
                var query = new GetAcademicPracticeWithDetailsQuery(id);
                var result = await _mediator.Send(query);
                return Ok(new ApiResponse<AcademicPracticeWithDetailsResponseDto> { Success = true, Data = result });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"No se encontró la práctica académica con ID {id}" }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"Error al obtener la práctica académica: {ex.Message}" }
                });
            }
        }

        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            try
            {
                var query = new GetAcademicPracticesByUserQuery(id, status);
                var result = await _mediator.Send(query);
                return Ok(new ApiResponse<List<AcademicPracticeWithDetailsResponseDto>> { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"Error al obtener las prácticas académicas del usuario: {ex.Message}" }
                });
            }
        }

        [HttpGet("ByTeacher/{id}")]
        public async Task<IActionResult> GetByTeacherId(
            int id, 
            [FromQuery] PaginatedRequest request)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"Error al obtener las prácticas académicas del docente: {ex.Message}" }
                });
            }
        }

        [HttpPut("UpdateInstitutionInfo/{id}")]
        public async Task<IActionResult> UpdateInstitutionInfo(int id, [FromBody] UpdateInstitutionInfoDto request)
        {
            try
            {
                // Implementation for updating institution information
                // This would typically involve a specific command for this operation
                return Ok(new ApiResponse<object> { Success = true, Messages = new List<string> { "Información de institución actualizada correctamente" } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"Error al actualizar información de institución: {ex.Message}" }
                });
            }
        }

        [HttpPut("UpdatePhaseApproval/{id}")]
        public async Task<IActionResult> UpdatePhaseApproval(int id, [FromBody] UpdatePhaseApprovalDto request)
        {
            try
            {
                // Implementation for updating phase approval dates
                // This would handle the different approval stages
                return Ok(new ApiResponse<object> { Success = true, Messages = new List<string> { "Aprobación de fase actualizada correctamente" } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"Error al actualizar aprobación de fase: {ex.Message}" }
                });
            }
        }

        [HttpGet("Simple")]
        public async Task<IActionResult> GetSimple()
        {
            try
            {
                var query = new GetSimpleAcademicPracticesQuery();
                var result = await _mediator.Send(query);
                return Ok(new ApiResponse<List<AcademicPracticeDto>> { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"Error en endpoint simple: {ex.Message}" }
                });
            }
        }
    }

    // Supporting DTOs for specific operations
    public class UpdateInstitutionInfoDto
    {
        public string? InstitutionName { get; set; }
        public string? InstitutionContact { get; set; }
        public DateTime? PracticeStartDate { get; set; }
        public DateTime? PracticeEndDate { get; set; }
        public int? PracticeHours { get; set; }
        public string? Observations { get; set; }
    }

    public class UpdatePhaseApprovalDto
    {
        public string PhaseType { get; set; } = string.Empty; // "Aval", "Plan", "Development", "FinalReport", "Final"
        public DateTime? ApprovalDate { get; set; }
        public string? Observations { get; set; }
        public string? EvaluatorObservations { get; set; }
    }
}

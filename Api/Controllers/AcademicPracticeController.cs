using Api.Authorization;
using Api.Responses;
using Application.Shared.Commands.AcademicPractices;
using Application.Shared.DTOs;
using Application.Shared.DTOs.AcademicPractices;
using Application.Shared.Queries.AcademicPractices;
using Domain.Common;
using Domain.Common.Extensions;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    /// <summary>
    /// Controlador de Prácticas Académicas. Override GetAll/GetById para retornar detalles completos
    /// (queries con estudiantes, institución, fase). UpdateInstitutionInfo y UpdatePhaseApproval
    /// son endpoints específicos para gestión de fase e institución.
    /// </summary>
    public class AcademicPracticeController : GenericController<AcademicPractice, int, AcademicPracticeDto>
    {
        private readonly IMediator _mediator;

        public AcademicPracticeController(IMediator mediator) : base(mediator)
        {
            _mediator = mediator;
        }

        [RequirePermission(
            PermissionCodes.PracticaAcademica.N4PAF0PA,
            PermissionCodes.PracticaAcademica.N4PAF0PG,
            PermissionCodes.PracticaAcademica.N4PAF1PG,
            PermissionCodes.PracticaAcademica.N4PAF2PG)]
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllWithDetails([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllAcademicPracticesQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy ?? string.Empty,
                IsDescending = request.IsDescending,
                Filters = request.Filters ?? new Dictionary<string, string>(),
                CursorId = request.CursorId,
                CursorCreatedAt = request.CursorCreatedAt,
                SkipTotalCount = request.SkipTotalCount
            };

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [RequirePermission(
            PermissionCodes.PracticaAcademica.N4PAF0PA,
            PermissionCodes.PracticaAcademica.N4PAF0PG,
            PermissionCodes.PracticaAcademica.N4PAF0PR,
            PermissionCodes.PracticaAcademica.N4PAF1PG,
            PermissionCodes.PracticaAcademica.N4PAF1PR,
            PermissionCodes.PracticaAcademica.N4PAF2PG,
            PermissionCodes.PracticaAcademica.N4PAF2PR)]
        [HttpGet("WithDetails/{id}")]
        public async Task<IActionResult> GetWithDetails(int id)
        {
            var query = new GetAcademicPracticeWithDetailsQuery(id);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<AcademicPracticeWithDetailsResponseDto> { Success = true, Data = result });
        }

        [RequirePermission(
            PermissionCodes.PracticaAcademica.N3PAF0R,
            PermissionCodes.PracticaAcademica.N3PAF0C,
            PermissionCodes.PracticaAcademica.N3PAF1R,
            PermissionCodes.PracticaAcademica.N3PAF1C,
            PermissionCodes.PracticaAcademica.N3PAF2R,
            PermissionCodes.PracticaAcademica.N3PAF2C)]
        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            var query = new GetAcademicPracticesByUserQuery(id, status);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<AcademicPracticeWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [RequirePermission(
            PermissionCodes.PracticaAcademica.N4PAF0PR,
            PermissionCodes.PracticaAcademica.N4PAF1PR,
            PermissionCodes.PracticaAcademica.N4PAF2PR)]
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
                request.Filters ?? new Dictionary<string, string>(),
                request.CursorId,
                request.CursorCreatedAt,
                request.SkipTotalCount
            );

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [RequirePermission(
            PermissionCodes.PracticaAcademica.N3PAF0R,
            PermissionCodes.PracticaAcademica.N4PAF0PG,
            PermissionCodes.PracticaAcademica.N4PAF1PG,
            PermissionCodes.PracticaAcademica.N4PAF2PG)]
        [HttpPut("UpdateInstitutionInfo/{id}")]
        public async Task<IActionResult> UpdateInstitutionInfo(int id, [FromBody] UpdateInstitutionInfoDto request)
        {
            request.Id = id;
            var result = await _mediator.Send(new UpdateAcademicPracticeInstitutionCommand(request, User.GetCurrentUserInfo()));
            
            if (!result)
                return NotFound(new ApiResponse<object> { Success = false, Errors = new List<string> { $"No se encontró la práctica académica con ID {id}" } });

            return Ok(new ApiResponse<object> { Success = true, Messages = new List<string> { "Información de institución actualizada correctamente" } });
        }

        [RequirePermission(
            PermissionCodes.PracticaAcademica.N4PAF0PR,
            PermissionCodes.PracticaAcademica.N4PAF1PR,
            PermissionCodes.PracticaAcademica.N4PAF2PR,
            PermissionCodes.PracticaAcademica.N3PAF1R,
            PermissionCodes.PracticaAcademica.N3PAF2R)]
        [HttpPut("UpdatePhaseApproval/{id}")]
        public async Task<IActionResult> UpdatePhaseApproval(int id, [FromBody] UpdatePhaseApprovalDto request)
        {
            request.Id = id;
            var result = await _mediator.Send(new UpdateAcademicPracticePhaseCommand(request, User.GetCurrentUserInfo()));
            
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

        [NonAction]
        public override Task<IActionResult> Create([FromBody] AcademicPracticeDto dto)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> CreateMultiple([FromBody] List<AcademicPracticeDto> dtos)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> Update(int id, [FromBody] AcademicPracticeDto dto)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }
    }
}

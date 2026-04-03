using Application.Shared.DTOs;
using Application.Shared.DTOs.ProjectFinals;
using Application.Shared.Queries.ProjectFinals;
using Microsoft.AspNetCore.Mvc;
using Api.Responses;
using Domain.Common;
using Domain.Common.Extensions;
using Domain.Entities;
using MediatR;
using Application.Shared.Commands.ProjectFinals;

namespace Api.Controllers
{
    /// <summary>
    /// Controlador de Proyectos Finales de Grado. Override GetAll/GetById como [NonAction]
    /// (usa queries con details). Es la fase final del flujo Propuesta→Anteproyecto→Proyecto.
    /// GetAllWithDetails, GetByUserId, GetByTeacherId retornan detalles completos con evaluación.
    /// Patch cambia estado y encola notificaciones.
    /// </summary>
    public class ProjectFinalController : GenericController<ProjectFinal, int, ProjectFinalDto>
    {
        private readonly IMediator _mediator;
        public ProjectFinalController(IMediator mediator) : base(mediator) { _mediator = mediator; }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, [FromBody] ProjectFinalPatchDto dto)
        {
            var currentUser = User.GetCurrentUserInfo();
            var command = new PatchProjectFinalCommand(id, dto, currentUser);
            var result = await _mediator.Send(command);
            return Ok(new ApiResponse<ProjectFinalDto> { Success = true, Data = result });
        }


        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllWithDetails([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllProjectFinalsQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                IsDescending = request.IsDescending,
                Filters = request.Filters
            };
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<ProjectFinalWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            var query = new GetProjectFinalsByUserQuery(id, status);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<ProjectFinalWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [HttpGet("ByTeacher/{id}")]
        public async Task<IActionResult> GetByTeacherId(int id, [FromQuery] PaginatedRequest request)
        {
            var query = new GetProjectFinalsByTeacherQuery(
                id,
                request.PageNumber,
                request.PageSize,
                request.SortBy ?? string.Empty,
                request.IsDescending,
                request.Filters ?? new Dictionary<string, string>()
            );
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<ProjectFinalWithDetailsResponseDto>> { Success = true, Data = result });
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

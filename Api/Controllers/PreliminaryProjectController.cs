using Application.Shared.DTOs.PreliminaryProjects;
using Application.Shared.Queries.PreliminaryProjects;
using Application.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Api.Responses;
using Domain.Common;
using Domain.Common.Extensions;
using Domain.Entities;
using MediatR;
using Application.Shared.Commands.PreliminaryProjects;

namespace Api.Controllers
{
    /// <summary>
    /// Controlador de Anteproyectos de Grado. Override GetAll/GetById como [NonAction]
    /// porque usa queries con details que retornan estudiantes, estado y evaluación.
    /// GetAllWithDetails, GetByUserId, GetByTeacherId son los endpoints de consulta.
    /// Patch cambia estado y encola notificaciones.
    /// </summary>
    public class PreliminaryProjectController : GenericController<PreliminaryProject, int, PreliminaryProjectDto>
    {
        private readonly IMediator _mediator;
        public PreliminaryProjectController(IMediator mediator) : base(mediator) { _mediator = mediator; }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, [FromBody] PreliminaryProjectPatchDto dto)
        {
            var currentUser = User.GetCurrentUserInfo();
            var command = new PatchPreliminaryProjectCommand(id, dto, currentUser);
            var result = await _mediator.Send(command);
            return Ok(new ApiResponse<PreliminaryProjectDto> { Success = true, Data = result });
        }


        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllWithDetails([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllPreliminaryProjectsQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                IsDescending = request.IsDescending,
                Filters = request.Filters
            };
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<PreliminaryProjectWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            var query = new GetPreliminaryProjectsByUserQuery(id, status);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<PreliminaryProjectWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [HttpGet("ByTeacher/{id}")]
        public async Task<IActionResult> GetByTeacherId(int id, [FromQuery] PaginatedRequest request)
        {
            var query = new GetPreliminaryProjectsByTeacherQuery(
                id,
                request.PageNumber,
                request.PageSize,
                request.SortBy ?? string.Empty,
                request.IsDescending,
                request.Filters ?? new Dictionary<string, string>()
            );
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<PreliminaryProjectWithDetailsResponseDto>> { Success = true, Data = result });
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

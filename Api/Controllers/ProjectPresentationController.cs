using Application.Shared.Commands.ProjectPresentations;
using Application.Shared.DTOs;
using Application.Shared.DTOs.ProjectPresentations;
using Application.Shared.Queries.ProjectPresentations;
using Microsoft.AspNetCore.Mvc;
using Api.Responses;
using Domain.Common;
using Domain.Common.Extensions;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    /// <summary>
    /// Controlador de Sustentaciones de Proyectos de Grado (ProjectPresentation).
    /// Fase de Sustentación según Acuerdo 015.
    /// </summary>
    public class ProjectPresentationController : GenericController<ProjectPresentation, int, ProjectPresentationDto>
    {
        private readonly IMediator _mediator;

        public ProjectPresentationController(IMediator mediator) : base(mediator)
        {
            _mediator = mediator;
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, [FromBody] ProjectPresentationPatchDto dto)
        {
            var currentUser = User.GetCurrentUserInfo();
            var command = new PatchProjectPresentationCommand(id, dto, currentUser);
            var result = await _mediator.Send(command);
            return Ok(new ApiResponse<ProjectPresentationDto> { Success = true, Data = result });
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllWithDetails([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllProjectPresentationsQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                IsDescending = request.IsDescending,
                Filters = request.Filters,
                CursorId = request.CursorId,
                CursorCreatedAt = request.CursorCreatedAt,
                SkipTotalCount = request.SkipTotalCount
            };
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<ProjectPresentationWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            var query = new GetProjectPresentationsByUserQuery(id, status);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<ProjectPresentationWithDetailsResponseDto>> { Success = true, Data = result });
        }

        [HttpGet("ByTeacher/{id}")]
        public async Task<IActionResult> GetByTeacherId(int id, [FromQuery] PaginatedRequest request)
        {
            var query = new GetProjectPresentationsByTeacherQuery(
                id,
                request.PageNumber,
                request.PageSize,
                request.SortBy ?? string.Empty,
                request.IsDescending,
                request.Filters ?? new Dictionary<string, string>()
            )
            {
                CursorId = request.CursorId,
                CursorCreatedAt = request.CursorCreatedAt,
                SkipTotalCount = request.SkipTotalCount
            };
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<ProjectPresentationWithDetailsResponseDto>> { Success = true, Data = result });
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

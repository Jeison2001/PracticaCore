using Api.Authorization;
using Api.Responses;
using Application.Shared.Commands.Seminars;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Seminars;
using Application.Shared.Queries.Seminars;
using Domain.Entities;
using Domain.Common;
using Domain.Common.Extensions;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class SeminarController : GenericController<Seminar, int, SeminarDto>
    {
        private readonly IMediator _mediator;

        public SeminarController(IMediator mediator) : base(mediator)
        {
            _mediator = mediator;
        }

        [RequirePermission(PermissionCodes.Seminario.N2SAR, PermissionCodes.Seminario.N3SASGR)]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, [FromBody] SeminarPatchDto dto)
        {
            var currentUser = User.GetCurrentUserInfo();
            var command = new PatchSeminarCommand(id, dto, currentUser);
            var result = await _mediator.Send(command);
            return Ok(new ApiResponse<SeminarDto> { Success = true, Data = result });
        }

        [RequirePermission(PermissionCodes.Seminario.N3SASGG, PermissionCodes.Seminario.N3SASGR)]
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllWithDetails([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllSeminarsQuery
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
            return Ok(new ApiResponse<PaginatedResult<SeminarWithDetailsDto>> { Success = true, Data = result });
        }

        [RequirePermission(PermissionCodes.Seminario.N3SASGG, PermissionCodes.Seminario.N3SASGR)]
        [HttpGet("WithDetails/{id}")]
        public async Task<IActionResult> GetWithDetails(int id)
        {
            var result = await _mediator.Send(new GetSeminarWithDetailsQuery(id));
            return Ok(new ApiResponse<SeminarWithDetailsDto> { Success = true, Data = result });
        }

        [RequirePermission(PermissionCodes.Seminario.N2SAR, PermissionCodes.Seminario.N2SAC)]
        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            var query = new GetSeminarsByUserQuery(id, status);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<SeminarWithDetailsDto>> { Success = true, Data = result });
        }

        [RequirePermission(PermissionCodes.Seminario.N3SASGR)]
        [HttpGet("ByTeacher/{id}")]
        public async Task<IActionResult> GetByTeacherId(int id, [FromQuery] PaginatedRequest request)
        {
            var query = new GetSeminarsByTeacherQuery(
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
            return Ok(new ApiResponse<PaginatedResult<SeminarWithDetailsDto>> { Success = true, Data = result });
        }

        [NonAction]
        public override Task<IActionResult> GetAll(PaginatedRequest request)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> GetById(int id)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> Create([FromBody] SeminarDto dto)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> CreateMultiple([FromBody] List<SeminarDto> dtos)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> Update(int id, [FromBody] SeminarDto dto)
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

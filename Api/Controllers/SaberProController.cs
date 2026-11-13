using Api.Authorization;
using Api.Responses;
using Application.Shared.Commands.SaberPros;
using Application.Shared.DTOs;
using Application.Shared.DTOs.SaberPros;
using Application.Shared.Queries.SaberPros;
using Domain.Entities;
using Domain.Common;
using Domain.Common.Extensions;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class SaberProController : GenericController<SaberPro, int, SaberProDto>
    {
        private readonly IMediator _mediator;

        public SaberProController(IMediator mediator) : base(mediator)
        {
            _mediator = mediator;
        }

        [RequirePermission(PermissionCodes.SaberPro.N2SPR, PermissionCodes.SaberPro.N3SPSGR)]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, [FromBody] SaberProPatchDto dto)
        {
            var currentUser = User.GetCurrentUserInfo();
            var command = new PatchSaberProCommand(id, dto, currentUser);
            var result = await _mediator.Send(command);
            return Ok(new ApiResponse<SaberProDto> { Success = true, Data = result });
        }

        [RequirePermission(PermissionCodes.SaberPro.N3SPSGG, PermissionCodes.SaberPro.N3SPSGR)]
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllWithDetails([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllSaberProsQuery
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
            return Ok(new ApiResponse<PaginatedResult<SaberProWithDetailsDto>> { Success = true, Data = result });
        }

        [RequirePermission(PermissionCodes.SaberPro.N3SPSGG, PermissionCodes.SaberPro.N3SPSGR)]
        [HttpGet("WithDetails/{id}")]
        public async Task<IActionResult> GetWithDetails(int id)
        {
            var result = await _mediator.Send(new GetSaberProWithDetailsQuery(id));
            return Ok(new ApiResponse<SaberProWithDetailsDto> { Success = true, Data = result });
        }

        [RequirePermission(PermissionCodes.SaberPro.N2SPR, PermissionCodes.SaberPro.N2SPC)]
        [HttpGet("ByUser/{id}")]
        public async Task<IActionResult> GetByUserId(int id, bool? status = null)
        {
            var query = new GetSaberProsByUserQuery(id, status);
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<List<SaberProWithDetailsDto>> { Success = true, Data = result });
        }

        [RequirePermission(PermissionCodes.SaberPro.N3SPSGR)]
        [HttpGet("ByTeacher/{id}")]
        public async Task<IActionResult> GetByTeacherId(
            int id,
            [FromQuery] PaginatedRequest request)
        {
            var query = new GetSaberProsByTeacherQuery(
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
            return Ok(new ApiResponse<PaginatedResult<SaberProWithDetailsDto>> { Success = true, Data = result });
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
        public override Task<IActionResult> Create([FromBody] SaberProDto dto)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> CreateMultiple([FromBody] List<SaberProDto> dtos)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> Update(int id, [FromBody] SaberProDto dto)
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

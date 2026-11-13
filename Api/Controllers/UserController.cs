using Api.Authorization;
using Api.Responses;
using Application.Common.Services.Jobs;
using Application.Shared.Commands;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Users;
using Application.Shared.Queries.Users;
using Domain.Common;
using Domain.Common.Auth;
using Domain.Common.Extensions;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Services.Jobs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class UserController : GenericController<User, int, UserDto>
    {
        private readonly IJobEnqueuer _jobEnqueuer;

        public UserController(IMediator mediator, IJobEnqueuer jobEnqueuer) : base(mediator)
        {
            _jobEnqueuer = jobEnqueuer;
        }

        [RequirePermission(PermissionCodes.Configuraciones.N2CFP, PermissionCodes.Configuraciones.N3CFSU)]
        [HttpGet("{id}")]
        public override async Task<IActionResult> GetById(int id)
        {
            if (IsCrossUserAccessDenied(id))
                return Forbid();

            return await base.GetById(id);
        }

        /// <summary>
        /// Obtiene usuarios paginados incluyendo sus roles activos asignados (batch fetch, zero N+1).
        /// </summary>
        [RequirePermission(PermissionCodes.Configuraciones.N3CFSU, PermissionCodes.ModalidadesGrado.N2MGR)]
        [HttpGet]
        public override async Task<IActionResult> GetAll([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllUsersWithRolesQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                IsDescending = request.IsDescending,
                Filters = request.Filters,
                CursorCreatedAt = request.CursorCreatedAt,
                CursorId = request.CursorId.HasValue ? (int)request.CursorId.Value : null,
                SkipTotalCount = request.SkipTotalCount
            };

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<UserDto>> { Success = true, Data = result });
        }

        /// <summary>
        /// Crea un usuario y encola la notificación institucional de bienvenida (USER_CREATED).
        /// </summary>
        [RequirePermission(PermissionCodes.Configuraciones.N3CFSU)]
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] UserDto dto)
        {
            var result = await _mediator.Send(new CreateEntityCommand<User, int, UserDto>(dto, User.GetCurrentUserInfo()));

            // Encolar job de notificación en segundo plano
            _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleUserCreationAsync(result.Id));

            return StatusCode(StatusCodes.Status201Created, new ApiResponse<UserDto> { Success = true, Data = result });
        }

        [NonAction]
        public override Task<IActionResult> CreateMultiple([FromBody] List<UserDto> dtos)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [RequirePermission(PermissionCodes.Configuraciones.N2CFP, PermissionCodes.Configuraciones.N3CFSU)]
        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] UserDto dto)
        {
            if (IsCrossUserAccessDenied(id))
                return Forbid();

            return await base.Update(id, dto);
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSU)]
        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto) => await base.UpdateStatus(id, dto);

        private bool IsCrossUserAccessDenied(int targetUserId)
        {
            if (User?.Identity?.IsAuthenticated != true)
                return false;

            var permissions = PermissionClaimHelper.ExtractPermissionCodes(User);
            if (permissions.Contains(PermissionCodes.Configuraciones.N3CFSU))
                return false;

            return User.GetUserId() != targetUserId;
        }
    }
}
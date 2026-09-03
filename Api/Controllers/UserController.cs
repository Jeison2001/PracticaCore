using Api.Responses;
using Application.Common.Services.Jobs;
using Application.Shared.Commands;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Users;
using Application.Shared.Queries.Users;
using Domain.Common;
using Domain.Common.Extensions;
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

        /// <summary>
        /// Obtiene usuarios paginados incluyendo sus roles activos asignados (batch fetch, zero N+1).
        /// </summary>
        [HttpGet]
        public override async Task<IActionResult> GetAll([FromQuery] PaginatedRequest request)
        {
            var query = new GetAllUsersWithRolesQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                IsDescending = request.IsDescending,
                Filters = request.Filters
            };

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<UserDto>> { Success = true, Data = result });
        }

        /// <summary>
        /// Crea un usuario y encola la notificación institucional de bienvenida (USER_CREATED).
        /// </summary>
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] UserDto dto)
        {
            var result = await _mediator.Send(new CreateEntityCommand<User, int, UserDto>(dto, User.GetCurrentUserInfo()));

            // Encolar job de notificación en segundo plano
            _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x => x.HandleUserCreationAsync(result.Id));

            return StatusCode(StatusCodes.Status201Created, new ApiResponse<UserDto> { Success = true, Data = result });
        }
    }
}
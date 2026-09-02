using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Users;
using Application.Shared.Queries.Users;
using Domain.Common;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class UserController : GenericController<User, int, UserDto>
    {
        public UserController(IMediator mediator) : base(mediator)
        {
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
    }
}
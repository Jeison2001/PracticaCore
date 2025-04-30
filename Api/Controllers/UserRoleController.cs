using Api.Controllers;
using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.UserRole;
using Application.Shared.Queries.UserRole;
using Domain.Common;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class UserRoleController : GenericController<UserRole, int, UserRoleDto>
    {
        private readonly IMediator _mediator;

        public UserRoleController(IMediator mediator) : base(mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Obtiene los usuarios por rol, con filtrado opcional por código de rol
        /// </summary>
        /// <param name="roleCode">Código del rol a filtrar (opcional)</param>
        /// <param name="request">Parámetros de paginación y filtros</param>
        /// <returns>Lista paginada de usuarios con el rol especificado</returns>
        [HttpGet("ByRole")]
        public async Task<IActionResult> GetUsersByRole([FromQuery] string? roleCode, [FromQuery] PaginatedRequest request)
        {
            // Corregir PageNumber si es menor a 1
            if (request.PageNumber < 1)
            {
                request.PageNumber = 1;
            }

            var query = new GetUserRolesByRoleCodeQuery
            {
                RoleCode = roleCode,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                IsDescending = request.IsDescending,
                Filters = request.Filters
            };

            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<PaginatedResult<UserRoleWithUserDetailsDto>> { Success = true, Data = result });
        }
    }
}
using Api.Responses;
using Application.Shared.Commands.UserRoles;
using Application.Shared.DTOs;
using Application.Shared.DTOs.UserRoles;
using Application.Shared.Queries.UserRoles;
using Domain.Common;
using Domain.Common.Extensions;
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
        /// Override del Create genérico: si el DTO trae cargos habilitados (TypeTeachingAssignmentIds)
        /// y el rol es TEACHER, los asigna en el mismo commit (operación atómica con el rol y sus permisos).
        /// </summary>
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] UserRoleDto dto)
        {
            var result = await _mediator.Send(new CreateUserRoleWithCargosCommand
            {
                Dto = dto,
                CurrentUser = User.GetCurrentUserInfo()
            });
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<UserRoleDto> { Success = true, Data = result });
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
        }        /// <summary>
        /// Obtiene los roles asignados a un usuario específico con información completa del rol
        /// </summary>
        /// <param name="id">ID del usuario</param>
        /// <returns>Lista de roles del usuario con información del registro de relación</returns>
        [HttpGet("ByUser/{id}")]
        public async Task<ActionResult<ApiResponse<List<UserRoleInfoDto>>>> GetUserRolesByUserId(int id)
        {
            if (id <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Errors = new List<string> { "El ID de usuario debe ser válido." } });

            var userRoles = await _mediator.Send(new GetUserRolesByUserIdQuery { UserId = id });
            if (userRoles == null || !userRoles.Any())
                return NotFound(new ApiResponse<object> { Success = false, Errors = new List<string> { $"No se encontraron registros de UserRole para el usuario con ID {id}." } });
            
            return Ok(new ApiResponse<List<UserRoleInfoDto>> { Success = true, Data = userRoles });
        }
    }
}
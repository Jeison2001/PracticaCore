using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.UserPermissions;
using Application.Shared.Queries.Permissions;
using Domain.Common;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class UserPermissionController : GenericController<UserPermission, int, UserPermissionDto>
    {
        private readonly IMediator _mediator;
        public UserPermissionController(IMediator mediator) : base(mediator)
        {
            _mediator = mediator;        }

        [HttpGet("ByUser/{id}")]
        public async Task<ActionResult<ApiResponse<List<UserPermissionInfoDto>>>> GetUserPermissionsByUserId(int id)
        {
            if (id <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Errors = new List<string> { "El ID de usuario debe ser válido." } });

            var permissions = await _mediator.Send(new GetPermissionsByUserIdQuery { UserId = id });
            if (permissions == null || !permissions.Any())
                return NotFound(new ApiResponse<object> { Success = false, Errors = new List<string> { $"No se encontraron permisos para el usuario con ID {id}." } });
            
            return Ok(new ApiResponse<List<UserPermissionInfoDto>> { Success = true, Data = permissions });
        }

        [NonAction]
        public override Task<IActionResult> GetById(int id)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> GetAll([FromQuery] PaginatedRequest request)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> Update(int id, [FromBody] UserPermissionDto dto)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }
    }
}
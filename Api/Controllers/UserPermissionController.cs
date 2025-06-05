using Application.Shared.DTOs.Permission;
using Application.Shared.DTOs.UserPermission;
using Application.Shared.Queries.Permission;
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
        public async Task<ActionResult<List<UserPermissionInfoDto>>> GetUserPermissionsByUserId(int id)
        {
            if (id <= 0)
                return BadRequest("El ID de usuario debe ser válido.");

            var permissions = await _mediator.Send(new GetPermissionsByUserIdQuery { UserId = id });
            if (permissions == null || !permissions.Any())
                return NotFound($"No se encontraron permisos para el usuario con ID {id}.");
            return Ok(permissions);
        }
    }
}
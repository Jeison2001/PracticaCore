using Api.Authorization;
using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.UserPermissions;
using Application.Shared.Queries.Permissions;
using Domain.Common;
using Domain.Common.Auth;
using Domain.Common.Extensions;
using Domain.Constants;
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

        [RequirePermission(PermissionCodes.Configuraciones.N2CFP, PermissionCodes.Configuraciones.N3CFSU, PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpGet("ByUser/{id}")]
        public async Task<ActionResult<ApiResponse<List<UserPermissionInfoDto>>>> GetUserPermissionsByUserId(int id)
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userPermissions = PermissionClaimHelper.ExtractPermissionCodes(User);
                var isAdmin = userPermissions.Contains(PermissionCodes.Configuraciones.N3CFSU)
                    || userPermissions.Contains(PermissionCodes.Configuraciones.N3CFSRL);
                if (!isAdmin && User.GetUserId() != id)
                    return Forbid();
            }

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

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSU, PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] UserPermissionDto dto) => await base.Create(dto);

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSU, PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpPost("multiple")]
        public override async Task<IActionResult> CreateMultiple([FromBody] List<UserPermissionDto> dtos) => await base.CreateMultiple(dtos);

        [NonAction]
        public override Task<IActionResult> Update(int id, [FromBody] UserPermissionDto dto)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFSU, PermissionCodes.Configuraciones.N3CFSRL)]
        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto) => await base.UpdateStatus(id, dto);
    }
}
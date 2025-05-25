using Api.Responses;
using Application.Shared.DTOs.RolePermission;
using Application.Shared.Queries.RolePermission;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class RolePermissionController : GenericController<RolePermission, int, RolePermissionDto>
    {
        private readonly IMediator _mediator;

        public RolePermissionController(IMediator mediator) : base(mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Obtiene los permisos asignados a un rol específico
        /// </summary>
        /// <param name="id">ID del rol (opcional)</param>
        /// <param name="code">Código del rol (opcional)</param>
        /// <returns>Lista de permisos del rol con información del registro de relación</returns>
        [HttpGet("ByRole")]
        public async Task<ActionResult<List<RolePermissionInfoDto>>> GetPermissionsByRole(
            [FromQuery] int? id = null, 
            [FromQuery] string? code = null)
        {
            // Validar que al menos uno de los parámetros sea proporcionado
            if (!id.HasValue && string.IsNullOrEmpty(code))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { "Debe proporcionar al menos el ID o el código del rol." }
                });
            }

            try
            {
                var permissions = await _mediator.Send(new GetPermissionsByRoleQuery(id, code));
                
                if (permissions == null || !permissions.Any())
                {
                    var roleIdentifier = id.HasValue ? $"ID {id}" : $"código '{code}'";
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Errors = new List<string> { $"No se encontraron permisos para el rol con {roleIdentifier}." }
                    });
                }
                
                return Ok(new ApiResponse<List<RolePermissionInfoDto>> 
                { 
                    Success = true, 
                    Data = permissions 
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"Error interno del servidor: {ex.Message}" }
                });
            }
        }
    }
}
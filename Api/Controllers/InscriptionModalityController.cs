using Application.Shared.Commands.InscriptionModalities;
using Application.Shared.DTOs.InscriptionModalities;
using Api.Responses;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers
{
    /// <summary>
    /// Handles InscriptionModality operations, including partial updates (PATCH)
    /// and status transitions via domain events.
    /// </summary>
    public class InscriptionModalityController : GenericController<InscriptionModality, int, InscriptionModalityDto>
    {
        public InscriptionModalityController(IMediator mediator) : base(mediator)
        {
        }

        [NonAction]
        public override Task<IActionResult> Update(int id, [FromBody] InscriptionModalityDto dto)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, [FromBody] InscriptionModalityPatchDto dto)
        {
            // Support both "sub" (JWT standard) and ClaimTypes.NameIdentifier (ASP.NET Core) claim types.
            var userIdClaim = User.FindFirst("sub")?.Value
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { "No se pudo identificar el usuario" }
                });
            }

            var command = new PatchInscriptionModalityCommand(id, dto, userId);
            var result = await _mediator.Send(command);

            return Ok(new ApiResponse<InscriptionModalityDto> { Success = true, Data = result });
        }
    }
}

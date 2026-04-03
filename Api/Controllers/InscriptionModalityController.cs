using Application.Shared.Commands.InscriptionModalities;
using Application.Shared.DTOs.InscriptionModalities;
using Api.Responses;
using Domain.Common.Extensions;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    /// <summary>
    /// Controlador de Inscripciones de Modalidad. Override Update como [NonAction]
    /// porque las transiciones de estado usan Patch (no PUT). Patch dispara
    /// PatchInscriptionModalityCommand que maneja aprobación/rechazo con validaciones
    /// de observación obligatoria y encola notificaciones según el nuevo estado.
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
            var currentUser = User.GetCurrentUserInfo();
            var command = new PatchInscriptionModalityCommand(id, dto, currentUser);
            var result = await _mediator.Send(command);

            return Ok(new ApiResponse<InscriptionModalityDto> { Success = true, Data = result });
        }
    }
}

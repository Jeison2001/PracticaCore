using Application.Shared.Commands.InscriptionModalities;
using Application.Shared.DTOs;
using Application.Shared.DTOs.InscriptionModalities;
using Api.Responses;
using Domain.Common;
using Domain.Common.Extensions;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    /// <summary>
    /// Controlador de Inscripciones de Modalidad. Cierra las operaciones CRUD genéricas como [NonAction]:
    /// las consultas y registros de inscripción usan InscriptionController y las transiciones usan Patch.
    /// </summary>
    public class InscriptionModalityController : GenericController<InscriptionModality, int, InscriptionModalityDto>
    {
        public InscriptionModalityController(IMediator mediator) : base(mediator)
        {
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, [FromBody] InscriptionModalityPatchDto dto)
        {
            var currentUser = User.GetCurrentUserInfo();
            var command = new PatchInscriptionModalityCommand(id, dto, currentUser);
            var result = await _mediator.Send(command);

            return Ok(new ApiResponse<InscriptionModalityDto> { Success = true, Data = result });
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
        public override Task<IActionResult> Create([FromBody] InscriptionModalityDto dto)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> CreateMultiple([FromBody] List<InscriptionModalityDto> dtos)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> Update(int id, [FromBody] InscriptionModalityDto dto)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequestDto dto)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }
    }
}

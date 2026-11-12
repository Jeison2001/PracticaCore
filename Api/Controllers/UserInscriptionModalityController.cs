using Application.Shared.DTOs;
using Application.Shared.DTOs.UserInscriptionModalities;
using Domain.Common;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    /// <summary>
    /// Controlador de participantes de inscripción. Cierra los endpoints CRUD genéricos con [NonAction]
    /// para evitar modificaciones directas fuera de InscriptionController.
    /// </summary>
    public class UserInscriptionModalityController : GenericController<UserInscriptionModality, int, UserInscriptionModalityDto>
    {
        public UserInscriptionModalityController(IMediator mediator) : base(mediator)
        {
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
        public override Task<IActionResult> Create([FromBody] UserInscriptionModalityDto dto)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> CreateMultiple([FromBody] List<UserInscriptionModalityDto> dtos)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        [NonAction]
        public override Task<IActionResult> Update(int id, [FromBody] UserInscriptionModalityDto dto)
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
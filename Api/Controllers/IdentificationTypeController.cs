using Api.Controllers;
using Application.Shared.DTOs.IdentificationType;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class IdentificationTypeController : GenericController<IdentificationType, int, IdentificationTypeDto>
    {
        public IdentificationTypeController(IMediator mediator) : base(mediator)
        {
        }
    }
}
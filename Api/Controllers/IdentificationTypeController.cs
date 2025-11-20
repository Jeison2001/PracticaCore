using Application.Shared.DTOs.IdentificationTypes;
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
using Api.Controllers;
using Application.Shared.DTOs.StageModality;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class StageModalityController : GenericController<StageModality, int, StageModalityDto>
    {
        public StageModalityController(IMediator mediator) : base(mediator)
        {
        }
    }
}

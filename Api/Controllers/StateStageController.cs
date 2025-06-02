using Api.Controllers;
using Application.Shared.DTOs.StateStage;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class StateStageController : GenericController<StateStage, int, StateStageDto>
    {
        public StateStageController(IMediator mediator) : base(mediator)
        {
        }
    }
}

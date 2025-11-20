using Application.Shared.DTOs.StateStages;
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

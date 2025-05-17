using Api.Controllers;
using Application.Shared.DTOs.StateProjectFinal;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class StateProjectFinalController : GenericController<StateProjectFinal, int, StateProjectFinalDto>
    {
        public StateProjectFinalController(IMediator mediator) : base(mediator) { }
    }
}

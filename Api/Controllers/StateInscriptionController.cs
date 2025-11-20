// filepath: c:\Users\LENOVO\source\repos\PracticaCore\Api\Controllers\StateInscriptionController.cs
using Application.Shared.DTOs.StateInscription;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class StateInscriptionController : GenericController<StateInscription, int, StateInscriptionDto>
    {
        public StateInscriptionController(IMediator mediator) : base(mediator)
        {
        }
    }
}
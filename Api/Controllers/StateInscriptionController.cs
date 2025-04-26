// filepath: c:\Users\LENOVO\source\repos\PracticaCore\Api\Controllers\StateInscriptionController.cs
using Api.Controllers;
using Application.Shared.DTOs.StateInscription;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class StateInscriptionController : GenericController<StateInscription, int, StateInscriptionDto>
    {
        public StateInscriptionController(IMediator mediator) : base(mediator)
        {
        }
    }
}
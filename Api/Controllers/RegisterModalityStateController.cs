using Api.Controllers;
using Application.Shared.DTOs.RegisterModalityState;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class RegisterModalityStateController : GenericController<RegisterModalityState, long, RegisterModalityStateDto>
    {
        public RegisterModalityStateController(IMediator mediator) : base(mediator)
        {
        }
    }
}
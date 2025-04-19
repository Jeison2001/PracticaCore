using Api.Controllers;
using Application.Shared.DTOs.RegisterModality;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class RegisterModalityController : GenericController<RegisterModality, long, RegisterModalityDto>
    {
        public RegisterModalityController(IMediator mediator) : base(mediator)
        {
        }
    }
}
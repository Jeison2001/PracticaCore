using Api.Controllers;
using Application.Shared.DTOs.InscriptionModality;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class InscriptionModalityController : GenericController<InscriptionModality, int, InscriptionModalityDto>
    {
        public InscriptionModalityController(IMediator mediator) : base(mediator)
        {
        }
    }
}
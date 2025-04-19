using Api.Controllers;
using Application.Shared.DTOs.Modality;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class ModalityController : GenericController<Modality, int, ModalityDto>
    {
        public ModalityController(IMediator mediator) : base(mediator)
        {
        }
    }
}
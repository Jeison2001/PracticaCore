using Api.Controllers;
using Application.Shared.DTOs.ResearchLine;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class ResearchLineController : GenericController<ResearchLine, int, ResearchLineDto>
    {
        public ResearchLineController(IMediator mediator) : base(mediator)
        {
        }
    }
}
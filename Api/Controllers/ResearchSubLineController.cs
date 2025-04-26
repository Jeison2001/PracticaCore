using Api.Controllers;
using Application.Shared.DTOs.ResearchSubLine;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class ResearchSubLineController : GenericController<ResearchSubLine, int, ResearchSubLineDto>
    {
        public ResearchSubLineController(IMediator mediator) : base(mediator)
        {
        }
    }
}
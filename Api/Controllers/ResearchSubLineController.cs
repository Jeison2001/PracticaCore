using Application.Shared.DTOs.ResearchSubLines;
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
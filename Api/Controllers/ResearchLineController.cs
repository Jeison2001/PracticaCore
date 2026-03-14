using Application.Shared.DTOs.ResearchLines;
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
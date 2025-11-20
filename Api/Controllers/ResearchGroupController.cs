using Application.Shared.DTOs.ResearchGroups;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class ResearchGroupController : GenericController<ResearchGroup, int, ResearchGroupDto>
    {
        public ResearchGroupController(IMediator mediator) : base(mediator)
        {
        }
    }
}
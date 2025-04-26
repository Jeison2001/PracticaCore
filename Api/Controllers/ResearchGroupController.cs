using Api.Controllers;
using Application.Shared.DTOs.ResearchGroup;
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
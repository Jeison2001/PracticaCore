using Application.Shared.DTOs.ThematicAreas;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class ThematicAreaController : GenericController<ThematicArea, int, ThematicAreaDto>
    {
        public ThematicAreaController(IMediator mediator) : base(mediator)
        {
        }
    }
}
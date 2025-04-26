using Api.Controllers;
using Application.Shared.DTOs.ThematicArea;
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
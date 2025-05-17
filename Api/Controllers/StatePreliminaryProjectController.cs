using Api.Controllers;
using Application.Shared.DTOs.StatePreliminaryProject;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class StatePreliminaryProjectController : GenericController<StatePreliminaryProject, int, StatePreliminaryProjectDto>
    {
        public StatePreliminaryProjectController(IMediator mediator) : base(mediator) { }
    }
}

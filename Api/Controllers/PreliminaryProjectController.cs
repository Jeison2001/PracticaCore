using Api.Controllers;
using Application.Shared.DTOs.PreliminaryProject;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class PreliminaryProjectController : GenericController<PreliminaryProject, int, PreliminaryProjectDto>
    {
        public PreliminaryProjectController(IMediator mediator) : base(mediator) { }
    }
}

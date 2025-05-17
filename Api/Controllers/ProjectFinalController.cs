using Api.Controllers;
using Application.Shared.DTOs.ProjectFinal;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class ProjectFinalController : GenericController<ProjectFinal, int, ProjectFinalDto>
    {
        public ProjectFinalController(IMediator mediator) : base(mediator) { }
    }
}

using Application.Shared.DTOs.StudyPlans;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class StudyPlanController : GenericController<StudyPlan, int, StudyPlanDto>
    {
        public StudyPlanController(IMediator mediator) : base(mediator)
        {
        }
    }
}

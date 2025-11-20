using Application.Shared.DTOs.Evaluation;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class EvaluationController : GenericController<Evaluation, int, EvaluationDto>
    {
        public EvaluationController(IMediator mediator) : base(mediator)
        {
        }
    }
}
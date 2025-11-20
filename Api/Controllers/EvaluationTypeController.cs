using Application.Shared.DTOs.EvaluationType;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class EvaluationTypeController : GenericController<EvaluationType, int, EvaluationTypeDto>
    {
        public EvaluationTypeController(IMediator mediator) : base(mediator)
        {
        }
    }
}
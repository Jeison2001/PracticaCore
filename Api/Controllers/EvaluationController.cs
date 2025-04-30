using Api.Controllers;
using Application.Shared.DTOs.Evaluation;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class EvaluationController : GenericController<Evaluation, int, EvaluationDto>
    {
        public EvaluationController(IMediator mediator) : base(mediator)
        {
        }
    }
}
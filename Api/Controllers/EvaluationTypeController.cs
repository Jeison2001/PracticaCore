using Api.Controllers;
using Application.Shared.DTOs.EvaluationType;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class EvaluationTypeController : GenericController<EvaluationType, int, EvaluationTypeDto>
    {
        public EvaluationTypeController(IMediator mediator) : base(mediator)
        {
        }
    }
}
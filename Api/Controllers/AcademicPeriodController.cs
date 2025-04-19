using Api.Controllers;
using Application.Shared.DTOs.AcademicPeriod;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class AcademicPeriodController : GenericController<AcademicPeriod, long, AcademicPeriodDto>
    {
        public AcademicPeriodController(IMediator mediator) : base(mediator)
        {
        }
    }
}
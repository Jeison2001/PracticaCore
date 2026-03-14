using Application.Shared.DTOs.AcademicPeriods;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class AcademicPeriodController : GenericController<AcademicPeriod, int, AcademicPeriodDto>
    {
        public AcademicPeriodController(IMediator mediator) : base(mediator)
        {
        }
    }
}
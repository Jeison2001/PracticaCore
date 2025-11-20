using Application.Shared.DTOs.AcademicPeriod;
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
using Application.Shared.DTOs.AcademicPrograms;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class AcademicProgramController : GenericController<AcademicProgram, int, AcademicProgramDto>
    {
        public AcademicProgramController(IMediator mediator) : base(mediator)
        {
        }
    }
}
using Api.Controllers;
using Application.Shared.DTOs.AcademicProgram;
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
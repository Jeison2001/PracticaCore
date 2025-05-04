using Api.Controllers;
using Application.Shared.DTOs.StatusWorkGrade;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class StatusWorkGradeController : GenericController<StatusWorkGrade, int, StatusWorkGradeDto>
    {
        public StatusWorkGradeController(IMediator mediator) : base(mediator)
        {
        }
    }
}

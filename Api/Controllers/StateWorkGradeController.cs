using Api.Controllers;
using Application.Shared.DTOs.StateWorkGrade;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class StateWorkGradeController : GenericController<StateWorkGrade, int, StateWorkGradeDto>
    {
        public StateWorkGradeController(IMediator mediator) : base(mediator)
        {
        }
    }
}

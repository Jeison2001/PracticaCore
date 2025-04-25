using Api.Controllers;
using Application.Shared.DTOs.Faculty;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class FacultyController : GenericController<Faculty, int, FacultyDto>
    {
        public FacultyController(IMediator mediator) : base(mediator)
        {
        }
    }
}
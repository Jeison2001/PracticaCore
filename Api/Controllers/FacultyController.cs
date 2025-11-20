using Application.Shared.DTOs.Faculties;
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
using Application.Shared.DTOs.TypeTeachingAssignments;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class TypeTeachingAssignmentController : GenericController<TypeTeachingAssignment, int, TypeTeachingAssignmentDto>
    {
        public TypeTeachingAssignmentController(IMediator mediator) : base(mediator)
        {
        }
    }
}
using Api.Controllers;
using Application.Shared.DTOs.TypeTeachingAssignment;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class TypeTeachingAssignmentController : GenericController<TypeTeachingAssignment, int, TypeTeachingAssignmentDto>
    {
        public TypeTeachingAssignmentController(IMediator mediator) : base(mediator)
        {
        }
    }
}
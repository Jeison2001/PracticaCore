using Api.Controllers;
using Application.Shared.DTOs.TeachingAssignment;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class TeachingAssignmentController : GenericController<TeachingAssignment, int, TeachingAssignmentDto>
    {
        public TeachingAssignmentController(IMediator mediator) : base(mediator)
        {
        }
    }
}
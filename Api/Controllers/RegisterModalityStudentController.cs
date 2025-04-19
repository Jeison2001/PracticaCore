using Api.Controllers;
using Application.Shared.DTOs.RegisterModalityStudent;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class RegisterModalityStudentController : GenericController<RegisterModalityStudent, int, RegisterModalityStudentDto>
    {
        public RegisterModalityStudentController(IMediator mediator) : base(mediator)
        {
        }
    }
}
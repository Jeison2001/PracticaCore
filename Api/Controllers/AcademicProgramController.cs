using Application.Shared.DTOs;
using Application.Shared.DTOs.AcademicPrograms;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class AcademicProgramController : GenericController<AcademicProgram, int, AcademicProgramDto>
    {
        public AcademicProgramController(IMediator mediator) : base(mediator)
        {
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public override async Task<IActionResult> GetById(int id) => await base.GetById(id);

        [AllowAnonymous]
        [HttpGet]
        public override async Task<IActionResult> GetAll([FromQuery] PaginatedRequest request) => await base.GetAll(request);
    }
}

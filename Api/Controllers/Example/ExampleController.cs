using Application.Shared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Example
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExampleController : GenericController<Domain.Entities.Example, int, ExampleDto>
    {
        public ExampleController(IMediator mediator) : base(mediator) { }
    }

}

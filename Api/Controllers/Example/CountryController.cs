using Application.Shared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Example
{
    [ApiController]
    [Route("api/[controller]")]
    public class CountryController : GenericController<Domain.Entities.Example, int, ExampleDto>
    {
        public CountryController(IMediator mediator) : base(mediator) { }
    }

}

using Api.Responses;
using Application.Common.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Example
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly IIdGeneratorService _idGeneratorService;

        public ServicesController(
            IIdGeneratorService idGeneratorService)
        {
            _idGeneratorService = idGeneratorService;
        }

        [HttpGet("generate-id")]
        public IActionResult GenerateId()
        {
            var result = new
            {
                UniqueId = _idGeneratorService.GenerateUniqueId(),
                Guid = _idGeneratorService.GenerateGuid()
            };

            return Ok(new ApiResponse<object> { Success = true, Data = result });
        }
    }
}
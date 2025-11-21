using Api.Responses;
using Application.Shared.DTOs.Auth;
using Domain.Interfaces.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("google")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthRequest request)
        {
            if (string.IsNullOrEmpty(request.IdToken))
                return BadRequest(new ApiResponse<object> { Success = false, Errors = new List<string> { "Token de autenticación requerido" } });

            var response = await _authService.AuthenticateWithGoogleAsync(request.IdToken);
            return Ok(new ApiResponse<AuthResponse> { Success = true, Data = response });
        }
    }
}
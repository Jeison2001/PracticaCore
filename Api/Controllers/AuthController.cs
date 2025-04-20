using Application.Shared.DTOs.Auth;
using Domain.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

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
            try
            {
                if (string.IsNullOrEmpty(request.IdToken))
                    return BadRequest("Token de autenticación requerido");

                var response = await _authService.AuthenticateWithGoogleAsync(request.IdToken);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }
    }
}
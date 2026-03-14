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

            var authResult = await _authService.AuthenticateWithGoogleAsync(request.IdToken);
            
            // Mapear de AuthenticationResult (Domain) a AuthResponse (Application/API)
            var response = new AuthResponse
            {
                Token = authResult.Token,
                User = new UserInfoDto
                {
                    Id = authResult.User.Id,
                    Email = authResult.User.Email,
                    FirstName = authResult.User.FirstName,
                    LastName = authResult.User.LastName,
                    Identification = authResult.User.Identification,
                    IdIdentificationType = authResult.User.IdIdentificationType
                },
                Roles = authResult.Roles.Select(r => new AuthRoleDto { Name = r.Name, Code = r.Code }).ToList(),
                Permissions = authResult.Permissions
            };
            
            return Ok(new ApiResponse<AuthResponse> { Success = true, Data = response });
        }
    }
}
using Api.Responses;
using Application.Shared.DTOs.Auth;
using AutoMapper;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Auth;
using Infrastructure.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly GoogleAuthService _googleAuthService;
        private readonly IUserInfoRepository _userInfoRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AuthController(
            IAuthService authService,
            GoogleAuthService googleAuthService,
            IUserInfoRepository userInfoRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _authService = authService;
            _googleAuthService = googleAuthService;
            _userInfoRepository = userInfoRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthRequest request)
        {
            if (string.IsNullOrEmpty(request.IdToken))
                return BadRequest(new ApiResponse<object> { Success = false, Errors = new List<string> { "Token de autenticación requerido" } });

            var authResult = await _googleAuthService.AuthenticateWithGoogleAsync(request.IdToken);
            var response = _mapper.Map<AuthResponse>(authResult);

            return Ok(new ApiResponse<AuthResponse> { Success = true, Data = response });
        }

        //Testing
        [HttpPost("manual")]
        public async Task<IActionResult> ManualLogin([FromBody] ManualAuthRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
                return BadRequest(new ApiResponse<object> { Success = false, Errors = new List<string> { "Email es requerido" } });

            try
            {
                var authResult = await _authService.AuthenticateManualAsync(request.Email, null);
                var response = _mapper.Map<AuthResponse>(authResult);

                return Ok(new ApiResponse<AuthResponse> { Success = true, Data = response });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Errors = new List<string> { ex.Message } });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var errors = new List<string>();

            if (request.IdIdentificationType <= 0)
                errors.Add("Tipo de identificación es requerido");
            if (string.IsNullOrEmpty(request.Identification))
                errors.Add("Número de identificación es requerido");
            if (string.IsNullOrEmpty(request.Email))
                errors.Add("Email es requerido");
            if (string.IsNullOrEmpty(request.FirstName))
                errors.Add("Primer nombre es requerido");
            if (string.IsNullOrEmpty(request.LastName))
                errors.Add("Apellido es requerido");
            if (request.RoleId <= 0)
                errors.Add("Rol es requerido");

            if (errors.Any())
                return BadRequest(new ApiResponse<object> { Success = false, Errors = errors });

            try
            {
                var existingUser = await _userInfoRepository.FindUserByEmailAsync(request.Email);
                if (existingUser != null)
                    return BadRequest(new ApiResponse<object> { Success = false, Errors = new List<string> { "El email ya está registrado" } });

                var existingByIdentification = await _userInfoRepository.FindUserByIdentificationAsync(request.Identification);
                if (existingByIdentification != null)
                    return BadRequest(new ApiResponse<object> { Success = false, Errors = new List<string> { "La identificación ya está registrada" } });

                var roleRepo = _unitOfWork.GetRepository<Domain.Entities.Role, int>();
                var role = await roleRepo.GetByIdAsync(request.RoleId);
                if (role == null)
                    return BadRequest(new ApiResponse<object> { Success = false, Errors = new List<string> { "Rol no encontrado" } });

                var user = new Domain.Entities.User
                {
                    IdIdentificationType = request.IdIdentificationType,
                    Identification = request.Identification,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    IdAcademicProgram = request.IdAcademicProgram,
                    StatusRegister = true,
                    OperationRegister = "User Registration"
                };

                var userRepo = _unitOfWork.GetRepository<Domain.Entities.User, int>();
                await userRepo.AddAsync(user);
                await _unitOfWork.CommitAsync();

                var userRole = new Domain.Entities.UserRole
                {
                    IdUser = user.Id,
                    IdRole = request.RoleId,
                    StatusRegister = true,
                    OperationRegister = "User Registration"
                };

                var userRoleRepo = _unitOfWork.GetRepository<Domain.Entities.UserRole, int>();
                await userRoleRepo.AddAsync(userRole);
                await _unitOfWork.CommitAsync();

                var response = new RegisterResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    Role = role.Name
                };

                return Ok(new ApiResponse<RegisterResponse> { Success = true, Data = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Errors = new List<string> { "Error al registrar usuario: " + ex.Message } });
            }
        }
    }
}

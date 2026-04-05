using Api.Responses;
using Application.Shared.DTOs.Auth;
using AutoMapper;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Auth;
using Infrastructure.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Api.Controllers
{
    /// <summary>
    /// Endpoints de autenticación: login Google OAuth2, login manual (testing), y registro de usuarios.
    /// GoogleLogin es el flujo principal de auth usado por pegi_web.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly GoogleAuthService _googleAuthService;
        private readonly IUserInfoRepository _userInfoRepository;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IJwtService _jwtService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly int _refreshTokenExpirationDays;

        public AuthController(
            IAuthService authService,
            GoogleAuthService googleAuthService,
            IUserInfoRepository userInfoRepository,
            IRefreshTokenService refreshTokenService,
            IJwtService jwtService,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IConfiguration configuration)
        {
            _authService = authService;
            _googleAuthService = googleAuthService;
            _userInfoRepository = userInfoRepository;
            _refreshTokenService = refreshTokenService;
            _jwtService = jwtService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;

            var rtExpireRaw = _configuration["Jwt:RefreshTokenExpirationDays"];
            _refreshTokenExpirationDays = int.TryParse(rtExpireRaw, out var days) && days > 0
                ? days
                : throw new InvalidOperationException($"Jwt:RefreshTokenExpirationDays tiene un valor inválido: '{rtExpireRaw}'. Debe ser un entero positivo.");
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthRequest request)
        {
            if (string.IsNullOrEmpty(request.IdToken))
                return BadRequest(new ApiResponse<object> { Success = false, Errors = new List<string> { "Token de autenticación requerido" } });

            var authResult = await _googleAuthService.AuthenticateWithGoogleAsync(request.IdToken);
            var response = _mapper.Map<AuthResponse>(authResult);

            await _refreshTokenService.PurgeExpiredAsync(response.User.Id);
            var refreshToken = await _refreshTokenService.GenerateAsync(response.User.Id);
            SetRefreshTokenCookie(refreshToken.Token);
            SetAccessTokenCookie(response.Token);

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

                await _refreshTokenService.PurgeExpiredAsync(response.User.Id);
                var refreshToken = await _refreshTokenService.GenerateAsync(response.User.Id);
                SetRefreshTokenCookie(refreshToken.Token);
                SetAccessTokenCookie(response.Token);

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
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshTokens()
        {
            var refreshTokenString = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshTokenString))
                return Unauthorized(new ApiResponse<object> { Success = false, Errors = new List<string> { "No se proporcionó refresh token." } });

            var rtEntity = await _refreshTokenService.ValidateTokenForReuseAsync(refreshTokenString);

            if (rtEntity == null)
                return Unauthorized(new ApiResponse<object> { Success = false, Errors = new List<string> { "Refresh token inválido." } });

            if (rtEntity.IsExpired)
                return Unauthorized(new ApiResponse<object> { Success = false, Errors = new List<string> { "Refresh token expirado." } });

            if (rtEntity.RevokedAt != null)
            {
                await _refreshTokenService.RevokeAllTokensForUserAsync(rtEntity.IdUser);
                Response.Cookies.Delete("accessToken");
                Response.Cookies.Delete("refreshToken");
                return Unauthorized(new ApiResponse<object> { Success = false, Errors = new List<string> { "Fraude o reúso de sesión detectado. Todas las sesiones han sido revocadas." } });
            }

            await _refreshTokenService.RevokeAsync(refreshTokenString);
            await _refreshTokenService.PurgeExpiredAsync(rtEntity.IdUser);
            var newRefreshToken = await _refreshTokenService.GenerateAsync(rtEntity.IdUser);
            SetRefreshTokenCookie(newRefreshToken.Token);

            var userRepo = _unitOfWork.GetRepository<Domain.Entities.User, int>();
            var user = await userRepo.GetByIdAsync(rtEntity.IdUser);
            if (user == null || !user.StatusRegister)
                return Unauthorized(new ApiResponse<object> { Success = false, Errors = new List<string> { "Usuario inválido o inactivo." } });

            var loginData = await _userInfoRepository.GetUserLoginDataAsync(user.Id);
            var permissionCodes = loginData.Permissions.Select(p => p.Code).ToList();
            var hierarchicalPermissions = BuildPermissionHierarchy(loginData.Permissions);

            string token = _jwtService.GenerateTokenWithClaims(
                user.Id.ToString(),
                user.Email,
                loginData.Roles.Select(r => r.Name).ToList(),
                permissionCodes,
                user.FirstName,
                user.LastName,
                user.Identification
            );

            var authResult = new Domain.Common.Auth.AuthenticationResult
            {
                Token = token,
                User = new Domain.Common.Auth.UserInfoResult
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Identification = user.Identification,
                    IdIdentificationType = user.IdIdentificationType
                },
                Roles = loginData.Roles,
                Permissions = hierarchicalPermissions
            };

            var response = _mapper.Map<AuthResponse>(authResult);
            SetAccessTokenCookie(response.Token);
            return Ok(new ApiResponse<AuthResponse> { Success = true, Data = response });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshTokenString = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshTokenString))
            {
                await _refreshTokenService.RevokeAsync(refreshTokenString);
                Response.Cookies.Delete("refreshToken");
                Response.Cookies.Delete("accessToken");
            }

            return Ok(new ApiResponse<object> { Success = true, Data = "Sesión cerrada correctamente." });
        }

        private void SetRefreshTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(_refreshTokenExpirationDays)
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private void SetAccessTokenCookie(string token)
        {
            var expireMinutes = _configuration["Jwt:ExpireMinutes"] is string raw && int.TryParse(raw, out var m) ? m : 15;
            Response.Cookies.Append("accessToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(expireMinutes)
            });
        }

        private Dictionary<string, object> BuildPermissionHierarchy(List<Domain.Common.Auth.PermissionInfo> permissions)
        {
            var result = new Dictionary<string, object>();
            var permissionsByCode = permissions.ToDictionary(p => p.Code, p => p);
            var rootPermissions = permissions
                .Where(p => string.IsNullOrEmpty(p.ParentCode) || !permissionsByCode.ContainsKey(p.ParentCode))
                .ToList();

            foreach (var rootPermission in rootPermissions)
            {
                result[rootPermission.Code] = BuildChildrenHierarchy(rootPermission.Code, permissions, permissionsByCode);
            }

            return result;
        }

        private object BuildChildrenHierarchy(string parentCode, List<Domain.Common.Auth.PermissionInfo> allPermissions, Dictionary<string, Domain.Common.Auth.PermissionInfo> permissionsByCode)
        {
            var children = allPermissions.Where(p => p.ParentCode == parentCode).ToList();

            if (!children.Any())
                return new List<string>();

            var childrenDict = new Dictionary<string, object>();
            foreach (var child in children)
            {
                childrenDict[child.Code] = BuildChildrenHierarchy(child.Code, allPermissions, permissionsByCode);
            }

            return childrenDict;
        }
    }
}

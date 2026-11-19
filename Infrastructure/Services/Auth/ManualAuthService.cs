using Domain.Common.Auth;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Auth;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Auth
{
    /// <summary>
    /// Servicio de autenticación manual para testing E2E.
    /// NO usar en producción.
    /// </summary>
    public class ManualAuthService : IManualAuthService
    {
        private readonly IJwtService _jwtService;
        private readonly IUserInfoRepository _userInfoRepository;
        private readonly ILogger<ManualAuthService> _logger;

        public ManualAuthService(
            IJwtService jwtService,
            IUserInfoRepository userInfoRepository,
            ILogger<ManualAuthService> logger)
        {
            _jwtService = jwtService;
            _userInfoRepository = userInfoRepository;
            _logger = logger;
        }

        public async Task<AuthenticationResult> AuthenticateManualAsync(string email, string? password)
        {
            var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;

            try
            {
                // Buscar usuario por email
                var user = await _userInfoRepository.FindUserByEmailAsync(normalizedEmail);

                if (user == null)
                {
                    // No auto-crear usuarios - solo usuarios pre-registrados pueden hacer login
                    _logger.LogWarning("Intento de login para usuario no registrado: {Email}", normalizedEmail);
                    throw new UnauthorizedAccessException("Usuario no registrado");
                }

                if (!user.StatusRegister)
                {
                    _logger.LogWarning("Intento de login manual para usuario inactivo: {Email}", normalizedEmail);
                    throw new UnauthorizedAccessException("Usuario inválido o inactivo.");
                }

                // Obtener datos de login (roles y permisos)
                var loginData = await _userInfoRepository.GetUserLoginDataAsync(user.Id);

                var permissionCodes = loginData.Permissions.Select(p => p.Code).ToList();
                var hierarchicalPermissions = BuildPermissionHierarchy(loginData.Permissions);

                // Generar JWT
                string token = _jwtService.GenerateTokenWithClaims(
                    user.Id.ToString(),
                    user.Email,
                    loginData.Roles.Select(r => r.Name).ToList(),
                    permissionCodes,
                    user.FirstName,
                    user.LastName,
                    user.Identification
                );

                _logger.LogInformation("Login manual exitoso para: {Email}", normalizedEmail);

                return new AuthenticationResult
                {
                    Token = token,
                    User = new UserInfoResult
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
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en login manual para: {Email}", normalizedEmail);
                throw new UnauthorizedAccessException("Error al autenticar el usuario.");
            }
        }

        private Dictionary<string, object> BuildPermissionHierarchy(List<PermissionInfo> permissions)
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

        private object BuildChildrenHierarchy(string parentCode, List<PermissionInfo> allPermissions, Dictionary<string, PermissionInfo> permissionsByCode)
        {
            var children = allPermissions.Where(p => p.ParentCode == parentCode).ToList();

            if (!children.Any())
            {
                return new List<string>();
            }

            var childrenDict = new Dictionary<string, object>();

            foreach (var child in children)
            {
                childrenDict[child.Code] = BuildChildrenHierarchy(child.Code, allPermissions, permissionsByCode);
            }

            return childrenDict;
        }
    }
}

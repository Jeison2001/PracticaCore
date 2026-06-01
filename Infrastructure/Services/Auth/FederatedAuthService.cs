using Domain.Common.Auth;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Auth;

namespace Infrastructure.Services.Auth
{
    /// <summary>
    /// Orquestador de autenticación federada agnóstico del proveedor.
    /// Valida el id_token a través de ITokenValidator (cuya implementación concreta
    /// se selecciona por configuración), resuelve el usuario y emite el JWT propio.
    /// </summary>
    public class FederatedAuthService : IAuthService
    {
        private readonly IJwtService _jwtService;
        private readonly IUserInfoRepository _userInfoRepository;
        private readonly IReadOnlyDictionary<string, ITokenValidator> _validators;

        public FederatedAuthService(IJwtService jwtService, IUserInfoRepository userInfoRepository, IEnumerable<ITokenValidator> tokenValidators)
        {
            _jwtService = jwtService;
            _userInfoRepository = userInfoRepository;

            // Indexa por proveedor; ante claves duplicadas, prevalece el último registrado.
            var map = new Dictionary<string, ITokenValidator>(StringComparer.OrdinalIgnoreCase);
            foreach (var validator in tokenValidators)
            {
                map[validator.Provider] = validator;
            }
            _validators = map;
        }

        public async Task<AuthenticationResult> AuthenticateWithTokenAsync(string idToken, string provider)
        {
            if (string.IsNullOrWhiteSpace(provider) || !_validators.TryGetValue(provider, out var tokenValidator))
            {
                throw new UnauthorizedAccessException(
                    $"Proveedor de autenticación '{provider}' no habilitado. Habilitados: {string.Join(", ", _validators.Keys)}");
            }

            try
            {
                var payload = await tokenValidator.ValidateAsync(idToken);

                var user = await _userInfoRepository.FindUserByEmailAsync(payload.Email);

                if (user == null)
                {
                    user = await _userInfoRepository.CreateUserIfNotExistsAsync(
                        payload.Email,
                        payload.GivenName,
                        payload.FamilyName);
                }

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
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException("Error al autenticar con el proveedor de identidad: " + ex.Message);
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

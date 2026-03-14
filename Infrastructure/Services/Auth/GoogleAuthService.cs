using Domain.Common.Auth;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Auth;

namespace Infrastructure.Services.Auth
{
    public class GoogleAuthService : IAuthService
    {
        private readonly IJwtService _jwtService;
        private readonly IUserInfoRepository _userInfoRepository;
        private readonly ITokenValidator _tokenValidator;

        public GoogleAuthService(IJwtService jwtService, IUserInfoRepository userInfoRepository, ITokenValidator tokenValidator)
        {
            _jwtService = jwtService;
            _userInfoRepository = userInfoRepository;
            _tokenValidator = tokenValidator;
        }

        public async Task<AuthenticationResult> AuthenticateWithGoogleAsync(string idToken)
        {
            try
            {
                var payload = await _tokenValidator.ValidateAsync(idToken);
                
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
                throw new UnauthorizedAccessException("Error al autenticar con Google: " + ex.Message);
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
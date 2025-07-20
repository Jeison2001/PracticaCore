using Application.Shared.DTOs.Auth;
using Domain.Entities;
using Domain.Interfaces.Auth;
using Google.Apis.Auth;

namespace Infrastructure.Services.Auth
{
    public class GoogleAuthService : IAuthService
    {
        private readonly IJwtService _jwtService;
        private readonly IUserInfoRepository _userInfoRepository;
        private const string InstitutionalDomain = "@unicesar.edu.co";

        public GoogleAuthService(IJwtService jwtService, IUserInfoRepository userInfoRepository)
        {
            _jwtService = jwtService;
            _userInfoRepository = userInfoRepository;
        }

        public async Task<dynamic> AuthenticateWithGoogleAsync(string idToken)
        {
            try
            {
                // Validar el token de Google
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
                
                // Verificar que el correo tenga el dominio institucional
                if (!payload.Email.EndsWith(InstitutionalDomain, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException($"Solo se permite el acceso con correos institucionales ({InstitutionalDomain})");
                }

                // Buscar usuario en la base de datos
                var user = await _userInfoRepository.FindUserByEmailAsync(payload.Email);
                
                // Si el usuario no existe, lanzar excepción
                if (user == null)
                {
                    throw new UnauthorizedAccessException("Usuario no registrado en el sistema. Contacte al administrador para crear su cuenta.");
                }
                
                /* Código comentado: Creación automática de usuarios
                // Buscar o crear al usuario en la base de datos
                var user = await _userInfoRepository.CreateUserIfNotExistsAsync(
                    payload.Email,
                    payload.GivenName,
                    payload.FamilyName);
                */

                // Obtener roles y permisos del usuario secuencialmente para evitar problemas de concurrencia con DbContext
                var roles = await _userInfoRepository.GetUserRolesAsync(user.Id);
                
                // Obtener información completa de permisos, incluyendo ParentCode
                var permissionsFullInfo = await _userInfoRepository.GetUserPermissionsFullInfoAsync(user.Id);
                
                // Extraer solo los códigos de permisos para el token
                var permissionCodes = permissionsFullInfo.Select(p => p.Code).ToList();
                
                // Construir la jerarquía de permisos utilizando la información completa (ParentCode)
                var hierarchicalPermissions = BuildPermissionHierarchy(permissionsFullInfo);

                // Generar token JWT con todos los datos del usuario
                string token = _jwtService.GenerateTokenWithClaims(
                    user.Id.ToString(), 
                    user.Email, 
                    roles, 
                    permissionCodes,
                    user.FirstName,
                    user.LastName,
                    user.Identification
                );                // Crear respuesta de autenticación con la estructura jerárquica
                return new AuthResponse
                {
                    Token = token,
                    User = new UserInfoDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Identification = user.Identification,
                        IdIdentificationType = user.IdIdentificationType
                    },
                    Roles = roles,
                    Permissions = hierarchicalPermissions
                };
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException("Error al autenticar con Google: " + ex.Message);
            }
        }

        /// <summary>
        /// Construye la jerarquía de permisos utilizando el campo ParentCode de los permisos.
        /// </summary>
        private Dictionary<string, object> BuildPermissionHierarchy(List<Permission> permissions)
        {
            var result = new Dictionary<string, object>();
            
            // Crear un diccionario para acceso rápido a permisos por código
            var permissionsByCode = permissions.ToDictionary(p => p.Code, p => p);
            
            // Identificar permisos raíz (los que no tienen ParentCode o su ParentCode no está en la lista)
            var rootPermissions = permissions
                .Where(p => string.IsNullOrEmpty(p.ParentCode) || !permissionsByCode.ContainsKey(p.ParentCode))
                .ToList();
            
            // Para cada permiso raíz, construir su subestructura recursivamente
            foreach (var rootPermission in rootPermissions)
            {
                result[rootPermission.Code] = BuildChildrenHierarchy(rootPermission.Code, permissions, permissionsByCode);
            }
            
            return result;
        }
        
        /// <summary>
        /// Construye recursivamente la jerarquía de los permisos hijos para un código de permiso dado.
        /// </summary>
        private object BuildChildrenHierarchy(string parentCode, List<Permission> allPermissions, Dictionary<string, Permission> permissionsByCode)
        {
            // Encontrar todos los hijos directos para este código de permiso
            var children = allPermissions.Where(p => p.ParentCode == parentCode).ToList();
            
            if (!children.Any())
            {
                return new List<string>(); // No tiene hijos
            }
            
            var childrenDict = new Dictionary<string, object>();
            
            // Para cada hijo, construir recursivamente su propia jerarquía
            foreach (var child in children)
            {
                childrenDict[child.Code] = BuildChildrenHierarchy(child.Code, allPermissions, permissionsByCode);
            }
            
            return childrenDict;
        }
    }
}
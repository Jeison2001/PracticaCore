using Application.Shared.DTOs.Auth;
using Domain.Interfaces.Auth;
using Google.Apis.Auth;

namespace Infrastructure.Services
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
                var permissions = await _userInfoRepository.GetUserPermissionsAsync(user.Id);
                
                // Construir la jerarquía de permisos directamente de los permisos del usuario
                // Sin necesidad de consultar todos los permisos del sistema
                var hierarchicalPermissions = GroupPermissionsByParentCode(permissions);

                // Generar token JWT con todos los datos del usuario
                string token = _jwtService.GenerateTokenWithClaims(
                    user.Id.ToString(), 
                    user.Email, 
                    roles, 
                    permissions,
                    user.FirstName,
                    user.LastName,
                    user.Identification
                );

                // Crear respuesta de autenticación con la estructura jerárquica
                return new AuthResponse
                {
                    Token = token,
                    User = new UserInfoDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Identification = user.Identification
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
        /// Agrupa los permisos jerárquicamente basándose solo en los códigos de los permisos
        /// sin necesidad de consultar la base de datos nuevamente.
        /// </summary>
        private Dictionary<string, object> GroupPermissionsByParentCode(List<string> permissions)
        {
            var result = new Dictionary<string, object>();
            
            // Asumiendo la convención de nombres: N1XX (nivel 1), N2XXX (nivel 2), N3XXXX (nivel 3), etc.
            // Donde los dos primeros caracteres (N1, N2, N3) indican el nivel
            
            // Agrupar permisos por su nivel (basado en su código)
            var permissionsByLevel = new Dictionary<string, List<string>>();
            
            foreach (var permission in permissions)
            {
                if (permission.Length >= 2)
                {
                    var level = permission.Substring(0, 2); // N1, N2, N3, etc.
                    if (!permissionsByLevel.ContainsKey(level))
                    {
                        permissionsByLevel[level] = new List<string>();
                    }
                    permissionsByLevel[level].Add(permission);
                }
            }
            
            // Identificar permisos de nivel 1 (N1XX)
            if (permissionsByLevel.ContainsKey("N1"))
            {
                foreach (var rootPermission in permissionsByLevel["N1"])
                {
                    // Extraer el código base sin el prefijo N1
                    var baseCode = rootPermission.Substring(2);
                    
                    // Buscar hijos en niveles inferiores
                    var children = GetChildrenRecursively(rootPermission, baseCode, permissionsByLevel);
                    
                    // Añadir a la jerarquía de resultados
                    result[rootPermission] = children.Count > 0 ? children : new List<string>();
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Busca recursivamente los permisos hijos basándose en la convención de nomenclatura.
        /// </summary>
        private Dictionary<string, object> GetChildrenRecursively(string parentCode, string baseCode, Dictionary<string, List<string>> permissionsByLevel)
        {
            var result = new Dictionary<string, object>();
            
            // Nivel actual (basado en los dos primeros caracteres del parentCode)
            var currentLevel = parentCode.Substring(0, 2);
            
            // Calcular el siguiente nivel (N1 -> N2, N2 -> N3, etc.)
            var nextLevelNum = int.Parse(currentLevel.Substring(1)) + 1;
            var nextLevel = "N" + nextLevelNum;
            
            // Verificar si tenemos permisos en el siguiente nivel
            if (permissionsByLevel.ContainsKey(nextLevel))
            {
                // Buscar hijos que empiecen con el baseCode
                foreach (var childPermission in permissionsByLevel[nextLevel])
                {
                    // Extraer el código base del hijo sin el prefijo del nivel
                    var childBaseCode = childPermission.Substring(2);
                    
                    // Verificar si este permiso es hijo del parentCode
                    if (childBaseCode.StartsWith(baseCode))
                    {
                        // Buscar recursivamente los hijos de este permiso
                        var grandChildren = GetChildrenRecursively(childPermission, childBaseCode, permissionsByLevel);
                        
                        // Añadir el hijo al resultado
                        result[childPermission] = grandChildren.Count > 0 ? grandChildren : new List<string>();
                    }
                }
            }
            
            return result;
        }
    }
}
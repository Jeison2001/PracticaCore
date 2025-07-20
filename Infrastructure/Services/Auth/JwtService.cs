using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Domain.Interfaces.Auth;
using Domain.Interfaces.Registration;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services.Auth
{
    /// <summary>
    /// Implementación del servicio de autorización JWT.
    /// Implementa ISingletonService para registrarse automáticamente como un servicio singleton.
    /// </summary>
    public class JwtService : IJwtService, ISingletonService
    {
        private readonly IConfiguration _configuration;
        private readonly HashSet<string> _revokedTokens;
        private readonly SigningCredentials _signingCredentials;
        private readonly TokenValidationParameters _validationParameters;
        private readonly string _issuer;
        private readonly string _audience;
        
        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
            _revokedTokens = new HashSet<string>();
            
            // Obtener configuración
            var jwtKey = _configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key no está configurado");
            _issuer = _configuration["Jwt:Issuer"] ?? "default-issuer";
            _audience = _configuration["Jwt:Audience"] ?? "default-audience";
            
            // Crear credenciales
            var key = Encoding.UTF8.GetBytes(jwtKey);
            var securityKey = new SymmetricSecurityKey(key);
            _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            
            // Configurar parámetros de validación
            _validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = securityKey,
                ClockSkew = TimeSpan.Zero
            };
        }

        public string GenerateTokenWithClaims(
            string userId, 
            string email, 
            IEnumerable<string> roles, 
            IEnumerable<string> permissions = null,
            string firstName = null,
            string lastName = null,
            string identification = null)
        {
            // Usar las credenciales de seguridad ya creadas en el constructor
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Agregar datos de usuario si están disponibles
            if (!string.IsNullOrEmpty(firstName))
                claims.Add(new Claim("firstName", firstName));
            
            if (!string.IsNullOrEmpty(lastName))
                claims.Add(new Claim("lastName", lastName));
            
            if (!string.IsNullOrEmpty(identification))
                claims.Add(new Claim("identification", identification));

            // Agregar roles como claims
            if (roles != null && roles.Any())
            {
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
                // Agregar array de roles
                claims.Add(new Claim("roles", JsonSerializer.Serialize(roles)));
            }

            // Agregar permisos como claims si están disponibles
            if (permissions != null && permissions.Any())
            {
                // Estructura jerárquica de permisos
                var hierarchicalPermissions = OrganizePermissionsHierarchy(permissions);
                claims.Add(new Claim("permissions", JsonSerializer.Serialize(hierarchicalPermissions)));
            }

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: _signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        
        /// <summary>
        /// Organiza los permisos en una estructura jerárquica basada en sus prefijos
        /// </summary>
        private Dictionary<string, object> OrganizePermissionsHierarchy(IEnumerable<string> permissions)
        {
            var result = new Dictionary<string, object>();
            var permissionsList = permissions.ToList(); // Convertir a lista para evitar múltiples enumeraciones
            
            // Agrupar permisos por nivel (N1, N2, N3, etc.)
            foreach (var permission in permissionsList)
            {
                // Ejemplo: N1PG, N2PGP, N3PGP
                if (permission.Length >= 2 && permission.StartsWith("N") && 
                    int.TryParse(permission.Substring(1, 1), out int level))
                {
                    string permCode = permission.Substring(2);   // PG, PGP, etc.

                    // Primer nivel (N1)
                    if (level == 1)
                    {
                        if (!result.ContainsKey(permission))
                        {
                            result[permission] = new List<object>();
                        }
                    }
                    // Niveles anidados (N2, N3, etc.)
                    else
                    {
                        // Buscar el permiso padre correcto
                        string parentPrefix = $"N{level-1}";
                        
                        // Para N3 buscar en N2, para N2 buscar en N1
                        string potentialParent = FindPotentialParent(permissionsList, parentPrefix, permCode);
                        
                        if (!string.IsNullOrEmpty(potentialParent))
                        {
                            // Asegurarse de que el padre existe en el diccionario
                            if (!result.ContainsKey(potentialParent))
                            {
                                result[potentialParent] = new List<object>();
                            }
                            
                            // Verificar si ya tenemos un elemento con esta clave en los hijos
                            var children = (List<object>)result[potentialParent];
                            
                            // Si es un hijo directo, agregarlo a la lista de hijos
                            if (!HasChild(children, permission))
                            {
                                // Si es N2, lo agregamos como string
                                if (level == 2)
                                {
                                    children.Add(permission);
                                }
                                // Si es N3 o superior, lo agregamos como un diccionario
                                else
                                {
                                    var childDict = new Dictionary<string, object>
                                    {
                                        { permission, new List<object>() }
                                    };
                                    children.Add(childDict);
                                }
                            }
                        }
                        // Si no encontramos un padre, lo agregamos al nivel principal
                        else
                        {
                            if (!result.ContainsKey(permission))
                            {
                                result[permission] = new List<object>();
                            }
                        }
                    }
                }
                else
                {
                    // Para permisos que no siguen el patrón N1, N2, etc.
                    if (!result.ContainsKey(permission))
                    {
                        result[permission] = new List<object>();
                    }
                }
            }
            
            return result;
        }

        private static string FindPotentialParent(List<string> permissions, string parentPrefix, string childCode)
        {
            // Para un código como N2PGP, buscamos un padre como N1PG
            return permissions.FirstOrDefault(perm => 
                perm.StartsWith(parentPrefix) && childCode.StartsWith(perm.Substring(2)));
        }

        private bool HasChild(List<object> children, string permission)
        {
            // Verificar si ya existe como string
            if (children.Contains(permission))
            {
                return true;
            }

            // Verificar si existe como clave en algún diccionario
            return children.OfType<Dictionary<string, object>>()
                           .Any(dict => dict.ContainsKey(permission));
        }
        
        public ClaimsPrincipal? ValidateToken(string token)
        {
            if (IsTokenRevoked(token))
                return null;
                
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, _validationParameters, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }
        
        public void RevokeToken(string token)
        {
            _revokedTokens.Add(token);
        }
        
        public bool IsTokenRevoked(string token)
        {
            return _revokedTokens.Contains(token);
        }
    }
}
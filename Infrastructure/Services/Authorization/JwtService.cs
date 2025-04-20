using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.Interfaces.Auth;
using Domain.Interfaces.Registration;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services.Authorization
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
        
        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
            _revokedTokens = new HashSet<string>();
            
            // Obtener configuración
            var jwtKey = _configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key no está configurado");
            var issuer = _configuration["Jwt:Issuer"] ?? "default-issuer";
            var audience = _configuration["Jwt:Audience"] ?? "default-audience";
            
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
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = securityKey,
                ClockSkew = TimeSpan.Zero
            };
        }
        
        public string GenerateToken(string userId, string email, IEnumerable<string> roles)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("La clave JWT no está configurada")));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Agregar roles como claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
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
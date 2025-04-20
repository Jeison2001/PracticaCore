using Application.Shared.DTOs.Auth;
using Domain.Entities;
using Domain.Interfaces.Auth;
using Google.Apis.Auth;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class GoogleAuthService : IAuthService
    {
        private readonly IJwtService _jwtService;
        private readonly IUserInfoRepository _userInfoRepository;

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
                
                // Verificar que el correo tenga el dominio @unicesar.edu.co
                if (!payload.Email.EndsWith("@unicesar.edu.co", StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Solo se permite el acceso con correos institucionales (@unicesar.edu.co)");
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

                // Obtener roles y permisos del usuario
                var roles = await _userInfoRepository.GetUserRolesAsync(user.Id);
                var permissions = await _userInfoRepository.GetUserPermissionsAsync(user.Id);

                // Generar token JWT
                string token = _jwtService.GenerateToken(user.Id.ToString(), user.Email, roles);

                // Crear respuesta de autenticación usando el DTO de Application
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
                    Permissions = permissions
                };
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException("Error al autenticar con Google: " + ex.Message);
            }
        }
    }
}
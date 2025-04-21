using Application.Shared.DTOs.Auth;
using Domain.Entities;
using Domain.Interfaces.Auth;
using Google.Apis.Auth;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

                // Generar token JWT con todos los datos del usuario y permisos jerárquicos
                string token = _jwtService.GenerateTokenWithClaims(
                    user.Id.ToString(), 
                    user.Email, 
                    roles, 
                    permissions,
                    user.FirstName,
                    user.LastName,
                    user.Identification
                );

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
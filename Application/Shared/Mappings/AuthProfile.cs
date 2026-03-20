using Application.Shared.DTOs.Auth;
using AutoMapper;
using Domain.Common.Auth;

namespace Application.Shared.Mappings
{
    public class AuthProfile : Profile
    {
        public AuthProfile()
        {
            // Todos los campos coinciden por nombre; AutoMapper los resuelve por convención
            CreateMap<UserInfoResult, UserInfoDto>();
            CreateMap<RoleInfoResult, AuthRoleDto>();
            CreateMap<AuthenticationResult, AuthResponse>();
        }
    }
}

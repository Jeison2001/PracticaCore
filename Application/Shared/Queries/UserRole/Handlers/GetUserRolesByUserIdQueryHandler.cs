using Application.Shared.DTOs.UserRole;
using Domain.Interfaces.Auth;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries.UserRole.Handlers
{
    public class GetUserRolesByUserIdQueryHandler : IRequestHandler<GetUserRolesByUserIdQuery, List<UserRoleInfoDto>>
    {
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IRepository<Domain.Entities.Role, int> _roleRepository;

        public GetUserRolesByUserIdQueryHandler(
            IUserRoleRepository userRoleRepository,
            IRepository<Domain.Entities.Role, int> roleRepository)
        {
            _userRoleRepository = userRoleRepository;
            _roleRepository = roleRepository;
        }

        public async Task<List<UserRoleInfoDto>> Handle(GetUserRolesByUserIdQuery request, CancellationToken cancellationToken)
        {
            // Obtener todos los UserRoles para el usuario
            var userRoles = await _userRoleRepository.GetUserRolesWithUserDetailsAsync(
                roleCode: null,
                pageNumber: 1,
                pageSize: int.MaxValue,
                sortBy: null,
                isDescending: false,
                filters: new Dictionary<string, string> { { "IdUser", request.UserId.ToString() } },
                cancellationToken: cancellationToken
            );

            var result = new List<UserRoleInfoDto>();

            foreach (var userRole in userRoles.Items)
            {
                // Obtener la información del rol
                var role = await _roleRepository.GetByIdAsync(userRole.IdRole);
                
                if (role != null)
                {
                    result.Add(new UserRoleInfoDto
                    {
                        // Propiedades del RoleDto (heredadas)
                        Id = role.Id,
                        Code = role.Code,
                        Name = role.Name,
                        Description = role.Description,
                        
                        // Propiedades adicionales específicas de UserRoleInfoDto
                        UserRoleId = userRole.Id, // ID del registro UserRole para operaciones
                        UserId = userRole.IdUser,
                        CreatedAt = userRole.CreatedAt,
                        IdUserCreatedAt = userRole.IdUserCreatedAt,
                        StatusRegister = userRole.StatusRegister,
                        OperationRegister = userRole.OperationRegister
                    });
                }
            }

            return result;
        }
    }
}

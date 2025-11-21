using Application.Shared.DTOs.Roles;
using Application.Shared.DTOs.UserRoles;
using MediatR;
using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.Shared.Queries.UserRoles.Handlers
{
    public class GetUserRolesByUserIdQueryHandler : IRequestHandler<GetUserRolesByUserIdQuery, List<UserRoleInfoDto>>
    {
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IRepository<Role, int> _roleRepository;

        public GetUserRolesByUserIdQueryHandler(
            IUserRoleRepository userRoleRepository,
            IRepository<Role, int> roleRepository)
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
                        Role = new RoleDto
                        {
                            Id = role.Id,
                            Code = role.Code,
                            Name = role.Name,
                            Description = role.Description,
                            IdUserCreatedAt = role.IdUserCreatedAt,
                            IdUserUpdatedAt = role.IdUserUpdatedAt,
                            StatusRegister = role.StatusRegister,
                            OperationRegister = role.OperationRegister
                        },
                        UserRole = new UserRoleDto
                        {
                            Id = userRole.Id, // ID del registro UserRole para operaciones
                            IdUser = userRole.IdUser,
                            IdRole = userRole.IdRole,
                            IdUserCreatedAt = userRole.IdUserCreatedAt,
                            IdUserUpdatedAt = userRole.IdUserUpdatedAt,
                            StatusRegister = userRole.StatusRegister,
                            OperationRegister = userRole.OperationRegister
                        }
                    });
                }
            }

            return result;
        }
    }
}
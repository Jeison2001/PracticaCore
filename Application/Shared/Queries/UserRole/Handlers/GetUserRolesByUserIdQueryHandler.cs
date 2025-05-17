using Application.Shared.DTOs.UserRole;
using Domain.Interfaces.Auth;
using MediatR;

namespace Application.Shared.Queries.UserRole.Handlers
{
    public class GetUserRolesByUserIdQueryHandler : IRequestHandler<GetUserRolesByUserIdQuery, List<UserRoleDto>>
    {
        private readonly IUserRoleRepository _userRoleRepository;
        public GetUserRolesByUserIdQueryHandler(IUserRoleRepository userRoleRepository)
        {
            _userRoleRepository = userRoleRepository;
        }
        public async Task<List<UserRoleDto>> Handle(GetUserRolesByUserIdQuery request, CancellationToken cancellationToken)
        {
            // Fetch all user roles for the user (direct assignments only)
            var userRoles = await _userRoleRepository.GetUserRolesWithUserDetailsAsync(
                roleCode: null,
                pageNumber: 1,
                pageSize: int.MaxValue,
                sortBy: null,
                isDescending: false,
                filters: new Dictionary<string, string> { { "IdUser", request.UserId.ToString() } },
                cancellationToken: cancellationToken
            );
            return userRoles.Items.Select(ur => new UserRoleDto
            {
                Id = ur.Id,
                IdUser = ur.IdUser,
                IdRole = ur.IdRole,
                IdUserCreatedAt = ur.IdUserCreatedAt
            }).ToList();
        }
    }
}

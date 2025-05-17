using Application.Shared.DTOs.UserPermission;
using Domain.Interfaces.Auth;
using MediatR;

namespace Application.Shared.Queries.UserPermission.Handlers
{
    public class GetUserPermissionsByUserIdQueryHandler : IRequestHandler<GetUserPermissionsByUserIdQuery, List<UserPermissionDto>>
    {
        private readonly IUserInfoRepository _userInfoRepository;
        public GetUserPermissionsByUserIdQueryHandler(IUserInfoRepository userInfoRepository)
        {
            _userInfoRepository = userInfoRepository;
        }
        public async Task<List<UserPermissionDto>> Handle(GetUserPermissionsByUserIdQuery request, CancellationToken cancellationToken)
        {
            // Get direct user permissions (UserPermission join entity)
            var userPermissions = await _userInfoRepository
                .GetUserPermissionsFullInfoAsync(request.UserId);
            // Return UserPermissionDto for each direct assignment (not for role-based permissions)
            var result = new List<UserPermissionDto>();
            foreach (var permission in userPermissions)
            {
                foreach (var up in permission.UserPermissions.Where(up => up.IdUser == request.UserId))
                {
                    result.Add(new UserPermissionDto
                    {
                        Id = up.Id,
                        IdUser = up.IdUser,
                        IdPermission = up.IdPermission,
                        IdUserCreatedAt = up.IdUserCreatedAt
                    });
                }
            }
            return result;
        }
    }
}

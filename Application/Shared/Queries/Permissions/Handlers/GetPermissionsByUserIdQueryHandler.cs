using Application.Shared.DTOs.Permissions;
using Application.Shared.DTOs.UserPermissions;
using MediatR;
using Domain.Interfaces.Repositories;

namespace Application.Shared.Queries.Permissions.Handlers
{
    public class GetPermissionsByUserIdQueryHandler : IRequestHandler<GetPermissionsByUserIdQuery, List<UserPermissionInfoDto>>
    {
        private readonly IUserInfoRepository _userInfoRepository;
        public GetPermissionsByUserIdQueryHandler(IUserInfoRepository userInfoRepository)
        {
            _userInfoRepository = userInfoRepository;
        }
        public async Task<List<UserPermissionInfoDto>> Handle(GetPermissionsByUserIdQuery request, CancellationToken cancellationToken)
        {
            var permissions = await _userInfoRepository.GetUserPermissionsFullInfoAsync(request.UserId);
            var result = new List<UserPermissionInfoDto>();
            
            foreach (var permission in permissions)
            {
                foreach (var up in permission.UserPermissions.Where(up => up.IdUser == request.UserId))
                {
                    result.Add(new UserPermissionInfoDto
                    {
                        Permission = new PermissionDto
                        {
                            Id = permission.Id,
                            Code = permission.Code,
                            ParentCode = permission.ParentCode,
                            Description = permission.Description,
                            IdUserCreatedAt = permission.IdUserCreatedAt,
                            IdUserUpdatedAt = permission.IdUserUpdatedAt,
                            UpdatedAt = permission.UpdatedAt,
                            StatusRegister = permission.StatusRegister,
                            OperationRegister = permission.OperationRegister
                        },
                        UserPermission = new UserPermissionDto
                        {
                            Id = up.Id,
                            IdUser = up.IdUser,
                            IdPermission = up.IdPermission,
                            IdUserCreatedAt = up.IdUserCreatedAt,
                            IdUserUpdatedAt = up.IdUserUpdatedAt,
                            UpdatedAt = up.UpdatedAt,
                            StatusRegister = up.StatusRegister,
                            OperationRegister = up.OperationRegister
                        }
                    });
                }
            }
            
            return result;
        }
    }
}
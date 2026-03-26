using Application.Shared.DTOs.Permissions;
using Application.Shared.DTOs.UserPermissions;
using Domain.Common.Auth;
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
            // Usa Include explícito para cargar Permission.UserPermissions - sin lazy loading
            var permissionsWithUserPermissions = await _userInfoRepository.GetUserPermissionsWithUserPermissionsAsync(request.UserId);

            return permissionsWithUserPermissions.Select(pwu => new UserPermissionInfoDto
            {
                Permission = new PermissionDto
                {
                    Id = pwu.Permission.Id,
                    Code = pwu.Permission.Code,
                    ParentCode = pwu.Permission.ParentCode,
                    Description = pwu.Permission.Description,
                    IdUserCreatedAt = pwu.Permission.IdUserCreatedAt,
                    IdUserUpdatedAt = pwu.Permission.IdUserUpdatedAt,
                    UpdatedAt = pwu.Permission.UpdatedAt,
                    StatusRegister = pwu.Permission.StatusRegister,
                    OperationRegister = pwu.Permission.OperationRegister
                },
                UserPermission = new UserPermissionDto
                {
                    Id = pwu.UserPermission.Id,
                    IdUser = pwu.UserPermission.IdUser,
                    IdPermission = pwu.UserPermission.IdPermission,
                    IdUserCreatedAt = pwu.UserPermission.IdUserCreatedAt,
                    IdUserUpdatedAt = pwu.UserPermission.IdUserUpdatedAt,
                    UpdatedAt = pwu.UserPermission.UpdatedAt,
                    StatusRegister = pwu.UserPermission.StatusRegister,
                    OperationRegister = pwu.UserPermission.OperationRegister
                }
            }).ToList();
        }
    }
}
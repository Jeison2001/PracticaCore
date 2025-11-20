using Application.Shared.DTOs.UserPermissions;
using MediatR;

namespace Application.Shared.Queries.Permissions
{
    public class GetPermissionsByUserIdQuery : IRequest<List<UserPermissionInfoDto>>
    {
        public int UserId { get; set; }
    }
}

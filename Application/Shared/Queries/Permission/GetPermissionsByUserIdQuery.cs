using Application.Shared.DTOs.UserPermission;
using MediatR;

namespace Application.Shared.Queries.Permission
{
    public class GetPermissionsByUserIdQuery : IRequest<List<UserPermissionInfoDto>>
    {
        public int UserId { get; set; }
    }
}

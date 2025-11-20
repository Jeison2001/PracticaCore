using Application.Shared.DTOs.UserPermissions;
using MediatR;

namespace Application.Shared.Queries.UserPermissions
{
    public class GetUserPermissionsByUserIdQuery : IRequest<List<UserPermissionDto>>
    {
        public int UserId { get; set; }
    }
}

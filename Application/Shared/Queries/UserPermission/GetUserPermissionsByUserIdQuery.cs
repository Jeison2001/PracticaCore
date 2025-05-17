using Application.Shared.DTOs.UserPermission;
using MediatR;

namespace Application.Shared.Queries.UserPermission
{
    public class GetUserPermissionsByUserIdQuery : IRequest<List<UserPermissionDto>>
    {
        public int UserId { get; set; }
    }
}

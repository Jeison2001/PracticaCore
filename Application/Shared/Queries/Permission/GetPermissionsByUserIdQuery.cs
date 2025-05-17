using Application.Shared.DTOs.Permission;
using MediatR;

namespace Application.Shared.Queries.Permission
{
    public class GetPermissionsByUserIdQuery : IRequest<List<PermissionDto>>
    {
        public int UserId { get; set; }
    }
}

using Application.Shared.DTOs.Role;
using MediatR;

namespace Application.Shared.Queries.Role
{
    public class GetRolesByUserIdQuery : IRequest<List<RoleDto>>
    {
        public int UserId { get; set; }
    }
}

using Application.Shared.DTOs.Roles;
using MediatR;

namespace Application.Shared.Queries.Roles
{
    public record GetRolesByUserIdQuery : IRequest<List<RoleDto>>
    {
        public int UserId { get; set; }
    }
}

using Application.Shared.DTOs.UserRoles;
using MediatR;

namespace Application.Shared.Queries.UserRoles
{
    public record GetUserRolesByUserIdQuery : IRequest<List<UserRoleInfoDto>>
    {
        public int UserId { get; set; }
    }
}

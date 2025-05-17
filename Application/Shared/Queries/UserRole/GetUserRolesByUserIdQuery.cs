using Application.Shared.DTOs.UserRole;
using MediatR;

namespace Application.Shared.Queries.UserRole
{
    public class GetUserRolesByUserIdQuery : IRequest<List<UserRoleDto>>
    {
        public int UserId { get; set; }
    }
}

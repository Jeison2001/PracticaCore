using Application.Shared.DTOs.UserRole;
using MediatR;

namespace Application.Shared.Queries.UserRole
{
    public class GetUserRolesByUserIdQuery : IRequest<List<UserRoleInfoDto>>
    {
        public int UserId { get; set; }
    }
}

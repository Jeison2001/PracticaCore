using Application.Shared.DTOs.RolePermission;
using MediatR;

namespace Application.Shared.Queries.RolePermission
{
    public class GetPermissionsByRoleQuery : IRequest<List<RolePermissionInfoDto>>
    {
        public int? RoleId { get; set; }
        public string? RoleCode { get; set; }

        public GetPermissionsByRoleQuery(int? roleId = null, string? roleCode = null)
        {
            RoleId = roleId;
            RoleCode = roleCode;
        }
    }
}

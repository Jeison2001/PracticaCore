using Application.Shared.DTOs.RolePermissions;
using MediatR;

namespace Application.Shared.Queries.RolePermissions
{
    public record GetPermissionsByRoleQuery : IRequest<List<RolePermissionInfoDto>>
    {
        public int? RoleId { get; set; }
        public string? RoleCode { get; set; }
        public bool? StatusRegister { get; set; }

        public GetPermissionsByRoleQuery(int? roleId = null, string? roleCode = null, bool? statusRegister = null)
        {
            RoleId = roleId;
            RoleCode = roleCode;
            StatusRegister = statusRegister;
        }
    }
}

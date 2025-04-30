using Application.Shared.DTOs.UserRole;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.UserRole
{
    public record GetUserRolesByRoleCodeQuery : IRequest<PaginatedResult<UserRoleWithUserDetailsDto>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? SortBy { get; init; }
        public bool IsDescending { get; init; } = false;
        public Dictionary<string, string>? Filters { get; init; }
        public string? RoleCode { get; init; }
    }
}
using Application.Shared.DTOs.Users;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.Users
{
    public record GetAllUsersWithRolesQuery : IRequest<PaginatedResult<UserDto>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? SortBy { get; init; }
        public bool IsDescending { get; init; } = false;
        public Dictionary<string, string>? Filters { get; init; }
    }
}

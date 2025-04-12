using Application.Shared.DTOs;
using Domain.Common;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Shared.Queries
{
    public record GetAllEntitiesQuery<T, TId, TDto> : IRequest<PaginatedResult<TDto>>
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? SortBy { get; init; }
        public bool IsDescending { get; init; } = false;
        public Dictionary<string, string>? Filters { get; init; }
    }
}

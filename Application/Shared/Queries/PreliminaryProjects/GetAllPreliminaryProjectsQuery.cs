using Application.Shared.DTOs.PreliminaryProjects;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.PreliminaryProjects
{
    public record GetAllPreliminaryProjectsQuery : IRequest<PaginatedResult<PreliminaryProjectWithDetailsResponseDto>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool IsDescending { get; set; }
        public Dictionary<string, string>? Filters { get; set; }
    }
}
